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
using System.Net;
using api.Encryption;

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
		public async Task<IActionResult> Reply() {
			using (var body = new StreamReader(Request.Body)) {
				var message = SnsMessage.ParseMessage(body.ReadToEnd());
				if (message.IsMessageSignatureValid()) {
					switch (message.Type) {
						case SnsMessage.MESSAGE_TYPE_NOTIFICATION:
							var notification = JsonConvert.DeserializeObject<SesReceiptNotification>(message.MessageText);
							var mailContent = MimeMessage
								.Load(
									stream: new MemoryStream(
										buffer: Encoding.UTF8.GetBytes(notification.Content)
									)
								)
								.GetTextBody(
									format: TextFormat.Plain
								);
							if (!String.IsNullOrWhiteSpace(mailContent)) {
								var addressMatch = notification.Mail.CommonHeaders.To
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
									var token = StringEncryption.Decrypt(
										text: UrlSafeBase64.Decode(addressMatch.Groups[1].Value),
										key: "UqlX9jyFSdvBe5/WYgGYUA=="
									);
									var content = WebUtility.HtmlEncode(mailContent);
									System.IO.File.WriteAllText(
										path: "mail-dump.txt",
										contents: token + @"\n\n" + content
									);
									return Ok();
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