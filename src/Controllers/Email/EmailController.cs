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

namespace api.Controllers.Email {
	public class EmailController : Controller {
		private DatabaseOptions dbOpts;
		public EmailController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Delivery() {
			using (var body = new StreamReader(Request.Body)) {
				var message = SnsMessage.ParseMessage(body.ReadToEnd());
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
							if ((await Program.HttpClient.GetAsync(message.SubscribeURL)).IsSuccessStatusCode) {
								return Ok();
							}
							break;
					}
				}
			}
			return BadRequest();
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Reply(
			[FromServices] NotificationService notificationService,
			[FromServices] CommentingService commentingService
		) {
			using (var body = new StreamReader(Request.Body)) {
				var message = SnsMessage.ParseMessage(body.ReadToEnd());
				if (message.IsMessageSignatureValid()) {
					switch (message.Type) {
						case SnsMessage.MESSAGE_TYPE_NOTIFICATION:
							var sesNotification = JsonConvert.DeserializeObject<SesReceiptNotification>(message.MessageText);
							var mailContent = MimeMessage
								.Load(
									stream: new MemoryStream(
										buffer: Encoding.UTF8.GetBytes(sesNotification.Content)
									)
								)
								.GetTextBody(
									format: TextFormat.Plain
								);
							// TODO: trim quoted text
							if (commentingService.IsCommentTextValid(mailContent)) {
								var addressMatch = sesNotification.Mail.CommonHeaders.To
									.Select(
										toString => Regex.Match(
											input: EmailFormatting.ExtractEmailAddress(toString),
											pattern: @"^reply\+([^@]+)@"
										)
									)
									.SingleOrDefault(
										match => match.Success
									);
								if (addressMatch != null) {
									var receiptId = notificationService
										.DecryptTokenString(
											tokenString: addressMatch.Groups[1].Value
										)
										.ReceiptId;
									using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
										var notification = await db.GetNotification(receiptId);
										var parent = await db.GetComment(notification.CommentIds.Single());
										var reply = await commentingService.PostComment(
											dbConnection: db,
											text: mailContent,
											articleId: parent.ArticleId,
											parentCommentId: parent.Id,
											userAccountId: notification.UserAccountId,
											analytics: new RequestAnalytics(
												client: new ClientAnalytics(
													type: ClientType.Mail,
													version: new SemanticVersion(0, 0, 0)
												)
											)
										);
										await db.CreateNotificationInteraction(
											receiptId: receiptId,
											channel: NotificationChannel.Email,
											action: NotificationAction.Reply,
											replyId: reply.Id
										);
										await db.ClearAlert(
											receiptId: receiptId
										);
										return Ok();
									}
								}
							}
							break;
						case SnsMessage.MESSAGE_TYPE_SUBSCRIPTION_CONFIRMATION:
							if ((await Program.HttpClient.GetAsync(message.SubscribeURL)).IsSuccessStatusCode) {
								return Ok();
							}
							break;
					}
				}
			}
			return BadRequest();
		}
	}
}