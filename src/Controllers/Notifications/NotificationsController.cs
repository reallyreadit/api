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
using System.Collections.Generic;

namespace api.Controllers.Notifications {
	public class NotificationsController : Controller {
		private readonly NotificationService notificationService;
		public NotificationsController(
			NotificationService notificationService
		) {
			this.notificationService = notificationService;
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AotdDigest(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromForm] AotdDigestForm form
		) {
			if (form.ApiKey == authOptions.Value.ApiKey) {
				await notificationService.CreateAotdDigestNotifications();
			}
			return Ok();
		}
		[HttpPost]
		public async Task<IActionResult> ClearAlerts(
			[FromBody] ClearAlertForm form
		) {
			NotificationEventType[] eventTypes;
			if (
				this.ClientVersionIsGreaterThanOrEqualTo(
					new Dictionary<ClientType, SemanticVersion>() {
						{ ClientType.WebAppClient, new SemanticVersion("1.30.0") }
					}
				)
			) {
				eventTypes = new Dictionary<Alert, NotificationEventType>() {
						{ Alert.Aotd, NotificationEventType.Aotd },
						{ Alert.Reply, NotificationEventType.Reply },
						{ Alert.Loopback, NotificationEventType.Loopback },
						{ Alert.Post, NotificationEventType.Post },
						{ Alert.Follower, NotificationEventType.Follower }
					}
					.Where(
						kvp => form.Alerts.HasFlag(kvp.Key)
					)
					.Select(
						kvp => kvp.Value
					)
					.ToArray();
			} else {
				switch (form.Alert) {
					case 0:
						eventTypes = new[] {
							NotificationEventType.Aotd
						};
						break;
					case 1:
						eventTypes = new[] {
							NotificationEventType.Follower
						};
						break;
					case 2:
						eventTypes = new[] {
							NotificationEventType.Post
						};
						break;
					case 3:
						eventTypes = new[] {
							NotificationEventType.Loopback,
							NotificationEventType.Reply
						};
						break;
					default:
						return BadRequest();
				}
			}
			await notificationService.ClearAlerts(
				User.GetUserAccountId(),
				eventTypes
			);
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
		[HttpPost]
		public async Task<IActionResult> FollowerDigest(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromForm] DigestForm form
		) {
			if (form.ApiKey == authOptions.Value.ApiKey) {
				await notificationService.CreateFollowerDigestNotifications(form.Frequency);
			}
			return Ok();
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> LoopbackDigest(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromForm] DigestForm form
		) {
			if (form.ApiKey == authOptions.Value.ApiKey) {
				await notificationService.CreateLoopbackDigestNotifications(form.Frequency);
			}
			return Ok();
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> PostDigest(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromForm] DigestForm form
		) {
			if (form.ApiKey == authOptions.Value.ApiKey) {
				await notificationService.CreatePostDigestNotifications(form.Frequency);
			}
			return Ok();
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
				var notification = await db.GetNotification(receiptId: obfuscation.DecodeSingle(form.ReceiptId).Value);
				if (notification.UserAccountId == userAccountId) {
					var parent = await db.GetComment(notification.CommentIds.Single());
					var article = await db.GetArticle(
						articleId: parent.ArticleId,
						userAccountId: userAccountId
					);
					if (article.IsRead) {
						var reply = await commentingService.PostReply(
							text: form.Text,
							articleId: parent.ArticleId,
							parentCommentId: parent.Id,
							userAccountId: notification.UserAccountId,
							analytics: new ClientAnalytics(
								type: ClientType.IosNotification,
								version: new SemanticVersion(0, 0, 0)
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
			await notificationService.ProcessPushView(
				receiptId: obfuscation.DecodeSingle(form.ReceiptId).Value,
				url: form.Url
			);
			return Ok();
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> ReplyDigest(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromForm] DigestForm form
		) {
			if (form.ApiKey == authOptions.Value.ApiKey) {
				await notificationService.CreateReplyDigestNotifications(form.Frequency);
			}
			return Ok();
		}
	}
}