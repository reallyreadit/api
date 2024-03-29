// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using SnsMessage = Amazon.SimpleNotificationService.Util.Message;
using SesDeliveryNotification = api.Messaging.AmazonSesNotifications.DeliveryNotification;
using SesReceiptNotification = api.Messaging.AmazonSesNotifications.ReceiptNotification;
using Newtonsoft.Json;
using Npgsql;
using api.DataAccess;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Text.RegularExpressions;
using api.Messaging;
using MimeKit;
using System.Text;
using MimeKit.Text;
using api.Notifications;
using api.Commenting;
using api.Analytics;
using api.DataAccess.Models;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace api.Controllers.Email {
	public class EmailController : Controller {
		private readonly DatabaseOptions dbOpts;
		private readonly IHttpClientFactory httpClientFactory;
		private readonly ILogger<EmailController> logger;
		public EmailController(
			IOptions<DatabaseOptions> dbOpts,
			IHttpClientFactory httpClientFactory,
			ILogger<EmailController> logger
		) {
			this.dbOpts = dbOpts.Value;
			this.httpClientFactory = httpClientFactory;
			this.logger = logger;
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Delivery() {
			using (var body = new StreamReader(Request.Body)) {
				var message = SnsMessage.ParseMessage(await body.ReadToEndAsync());
				if (message.IsMessageSignatureValid()) {
					switch (message.Type) {
						case SnsMessage.MESSAGE_TYPE_NOTIFICATION:
							using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
								var notification = JsonConvert.DeserializeObject<SesDeliveryNotification>(message.MessageText);
								await db.CreateEmailNotification(
									notificationType: notification.NotificationType,
									mail: notification.Mail,
									bounce: notification.Bounce,
									complaint: notification.Complaint
								);
								return Ok();
							}
						case SnsMessage.MESSAGE_TYPE_SUBSCRIPTION_CONFIRMATION:
							try {
								if ((await httpClientFactory.CreateClient().GetAsync(message.SubscribeURL)).IsSuccessStatusCode) {
									return Ok();
								}
							} catch (Exception ex) {
								logger.LogError(ex, "HttpClient error during email delivery SNS subscription confirmation.");
							}
							break;
					}
				}
			}
			return BadRequest();
		}
		[AllowAnonymous]
		public async Task<IActionResult> Link(
			[FromServices] NotificationService notificationService,
			string id
		) {
			return Json(
				new {
					Url = (await notificationService.ProcessEmailLink(id)).ToString()
				}
			);
		}
		[AllowAnonymous]
		public async Task<FileResult> Open(
			[FromServices] NotificationService notificationService,
			string id
		) {
			await notificationService.ProcessEmailOpen(id);
			return File(
				fileContents: Convert.FromBase64String(
					s: "R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw=="
				),
				contentType: "image/gif"
			);
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Reply(
			[FromServices] NotificationService notificationService,
			[FromServices] CommentingService commentingService
		) {
			using (var body = new StreamReader(Request.Body)) {
				var message = SnsMessage.ParseMessage(await body.ReadToEndAsync());
				if (message.IsMessageSignatureValid()) {
					switch (message.Type) {
						case SnsMessage.MESSAGE_TYPE_NOTIFICATION:
							var sesNotification = JsonConvert.DeserializeObject<SesReceiptNotification>(message.MessageText);
							var mailContent = String.Join(
								separator: '\n',
								values: new QuoteParser.QuoteParser
									.Builder()
									.Build()
									.Parse(
										(
											await MimeMessage.LoadAsync(
												stream: new MemoryStream(
													buffer: Encoding.UTF8.GetBytes(sesNotification.Content)
												)
											)
										)
										.GetTextBody(
											format: TextFormat.Plain
										)
									)
									.Body
								);
							if (commentingService.IsCommentTextValid(mailContent)) {
								var receiptId = sesNotification.Mail.CommonHeaders.To
									.Select(notificationService.GetReceiptIdFromEmailReplyAddress)
									.SingleOrDefault(
										receiptId => receiptId.HasValue
									);
								if (receiptId.HasValue) {
									Notification notification;
									Comment parent;
									using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
										notification = await db.GetNotification(receiptId.Value);
										parent = await db.GetComment(notification.CommentIds.Single());
									}
									var reply = await commentingService.PostReply(
										text: mailContent,
										articleId: parent.ArticleId,
										parentCommentId: parent.Id,
										userAccountId: notification.UserAccountId,
										analytics: new ClientAnalytics(
											type: ClientType.Mail,
											version: new SemanticVersion(0, 0, 0)
										)
									);
									await notificationService.ProcessEmailReply(
										userAccountId: notification.UserAccountId,
										receiptId: receiptId.Value,
										replyId: reply.Id
									);
									return Ok();
								}
							}
							break;
						case SnsMessage.MESSAGE_TYPE_SUBSCRIPTION_CONFIRMATION:
							try {
								if ((await httpClientFactory.CreateClient().GetAsync(message.SubscribeURL)).IsSuccessStatusCode) {
									return Ok();
								}
							} catch (Exception ex) {
								logger.LogError(ex, "HttpClient error during email reply SNS subscription confirmation.");
							}
							break;
					}
				}
			}
			return BadRequest();
		}
	}
}