using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using api.DataAccess;
using api.Authentication;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using api.Notifications;
using System;
using api.Encryption;

namespace api.Controllers.Notifications {
	public class NotificationsController : Controller {
		private readonly DatabaseOptions dbOpts;
		public NotificationsController(
			IOptions<DatabaseOptions> dbOpts
		) {
			this.dbOpts = dbOpts.Value;
		}
		[HttpPost]
		public async Task<IActionResult> ClearAlerts(
			[FromBody] ClearAlertForm form
		) {
			using (
				var db = new NpgsqlConnection(
					connectionString: dbOpts.ConnectionString
				)
			) {
				NotificationEventType[] types;
				var userAccountId = User.GetUserAccountId();
				switch (form.Alert) {
					case Alert.Aotd:
						types = new NotificationEventType[0];
						await db.ClearAotdAlert(
							userAccountId: userAccountId
						);	
						break;
					case Alert.Followers:
						types = new [] {
							NotificationEventType.Follower
						};
						break;
					case Alert.Following:
						types = new [] {
							NotificationEventType.Post
						};
						break;
					case Alert.Inbox:
						types = new [] {
							NotificationEventType.Reply,
							NotificationEventType.Loopback
						};
						break;
					default:
						return BadRequest();
				}
				foreach (var type in types) {
					await db.ClearAllAlerts(
						type: type,
						userAccountId: userAccountId
					);
				}
				return Ok();
			}
		}
		[HttpPost]
		public async Task<IActionResult> DeviceRegistration(
			[FromBody] DeviceRegistrationForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await db.RegisterNotificationPushDevice(
					userAccountId: User.GetUserAccountId(),
					installationId: form.InstallationId,
					name: form.Name,
					token: form.Token
				);
				return Ok();
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Index(
			[FromServices] NotificationService notificationService,
			string tokenString
		) {
			var token = notificationService.DecryptTokenString(tokenString);
			if (token.Channel.HasValue && token.Action.HasValue) {
				using (
					var db = new NpgsqlConnection(
						connectionString: dbOpts.ConnectionString
					)
				) {
					switch (token.Action) {
						case NotificationAction.Open:
							await notificationService.CreateOpenInteraction(
								db: db,
								receiptId: token.ReceiptId
							);
							return File(
								fileContents: Convert.FromBase64String(
									s: "R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw=="
								),
								contentType: "image/gif"
							);
						case NotificationAction.View:
							return Redirect(
								url: await notificationService.CreateViewInteraction(
									db: db,
									notification: await db.GetNotification(token.ReceiptId),
									channel: token.Channel.Value,
									resource: token.ViewActionResource.Value,
									resourceId: token.ViewActionResourceId.Value
								)
							);
					}
				}
			}
			return BadRequest();
		}
		[HttpPost]
		public async Task<IActionResult> PushAuthDenial(
			[FromBody] PushAuthDenialForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await db.CreateNotificationPushAuthDenial(
					userAccountId: User.GetUserAccountId(),
					installationId: form.InstallationId,
					deviceName: form.DeviceName
				);
				return Ok();
			}
		}
	}
}