using System;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using SnsMessage = Amazon.SimpleNotificationService.Util.Message;
using SesNotification = api.Messaging.AmazonSesNotifications.Notification;
using Newtonsoft.Json;
using Npgsql;
using api.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Serialization;

namespace api.Controllers.Email {
	public class EmailController : Controller {
		private DatabaseOptions dbOpts;
		public EmailController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Notification() {
			using (var body = new StreamReader(Request.Body)) {
				var message = SnsMessage.ParseMessage(body.ReadToEnd());
				if (message.IsMessageSignatureValid()) {
					switch (message.Type) {
						case SnsMessage.MESSAGE_TYPE_NOTIFICATION:
							using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
								var notification = JsonConvert.DeserializeObject<SesNotification>(message.MessageText);
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
	}
}