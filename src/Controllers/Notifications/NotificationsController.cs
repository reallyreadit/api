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
using api.Commenting;
using System.Linq;
using api.Analytics;

namespace api.Controllers.Notifications {
	public class NotificationsController : Controller {
		private readonly NotificationService notificationService;
		public NotificationsController(
			NotificationService notificationService
		) {
			this.notificationService = notificationService;
		}
		[HttpPost]
		public async Task<IActionResult> ClearAlerts(
			[FromBody] ClearAlertForm form
		) {
			var userAccountId = User.GetUserAccountId();
			switch (form.Alert) {
				case Alert.Aotd:
					await notificationService.ClearAlerts(
						userAccountId: userAccountId,
						types: NotificationEventType.Aotd
					);
					break;
				case Alert.Followers:
					await notificationService.ClearAlerts(
						userAccountId: userAccountId,
						types: NotificationEventType.Follower
					);
					break;
				case Alert.Following:
					await notificationService.ClearAlerts(
						userAccountId: userAccountId,
						types: NotificationEventType.Post
					);
					break;
				case Alert.Inbox:
					await notificationService.ClearAlerts(
						userAccountId: userAccountId,
						NotificationEventType.Loopback, NotificationEventType.Reply
					);
					break;
				default:
					return BadRequest();
			}
			return Ok();
		}
		[HttpPost]
		public async Task<IActionResult> DeviceRegistration(
			[FromServices] IOptions<DatabaseOptions> databaseOptions,
			[FromBody] DeviceRegistrationForm form
		) {
			var userAccountId = User.GetUserAccountId();
			await notificationService.RegisterPushDevice(
				userAccountId: userAccountId,
				installationId: form.InstallationId,
				name: form.Name,
				token: form.Token
			);
			using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
				return Json(
					await db.GetUserAccountById(userAccountId)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Index(
			[FromServices] NotificationService notificationService,
			string tokenString
		) {
			var result = await notificationService.ProcessEmailRequest(tokenString);
			if (result.HasValue) {
				switch (result.Value.Action) {
					case NotificationAction.Open:
						return File(
							fileContents: Convert.FromBase64String(
								s: "R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw=="
							),
							contentType: "image/gif"
						);
					case NotificationAction.View:
						return Redirect(result.Value.RedirectUrl.ToString());		
				}
			}
			return BadRequest();
		}
		[HttpPost]
		public async Task<IActionResult> PushAuthDenial(
			[FromBody] PushAuthDenialForm form
		) {
			await notificationService.LogAuthDenial(
				userAccountId: User.GetUserAccountId(),
				installationId: form.InstallationId,
				deviceName: form.DeviceName
			);
			return Ok();
		}
		[HttpPost]
		public async Task<IActionResult> PushReply(
			[FromServices] IOptions<DatabaseOptions> databaseOptions,
			[FromServices] ObfuscationService obfuscation,
			[FromServices] CommentingService commentingService,
			[FromBody] PushReplyForm form
		) {
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
				var notification = await db.GetNotification(receiptId: obfuscation.Decode(form.ReceiptId).Value);
				if (notification.UserAccountId == userAccountId) {
					var parent = await db.GetComment(notification.CommentIds.Single());
					var article = await db.GetArticle(
						articleId: parent.ArticleId,
						userAccountId: userAccountId
					);
					if (article.IsRead) {
						var reply = await commentingService.PostComment(
							dbConnection: db,
							text: form.Text,
							articleId: parent.ArticleId,
							parentCommentId: parent.Id,
							userAccountId: notification.UserAccountId,
							analytics: new RequestAnalytics(
								client: new ClientAnalytics(
									type: ClientType.IosNotification,
									version: new SemanticVersion(0, 0, 0)
								)
							)
						);
						await notificationService.ProcessPushReply(
							userAccountId: notification.UserAccountId,
							receiptId: notification.ReceiptId,
							replyId: reply.Id
						);
					}
				}
			}
			return Ok();
		}
		[HttpPost]
		public async Task<IActionResult> PushView(
			[FromServices] IOptions<DatabaseOptions> databaseOptions,
			[FromServices] ObfuscationService obfuscation,
			[FromBody] PushViewForm form
		) {
			using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
				var notification = await db.GetNotification(receiptId: obfuscation.Decode(form.ReceiptId).Value);
				if (notification.UserAccountId == User.GetUserAccountId()) {
					await notificationService.ProcessPushRequest(
						notification: notification,
						url: form.Url
					);
				}
			}
			return Ok();
		}
	}
}