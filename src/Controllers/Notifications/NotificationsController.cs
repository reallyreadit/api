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
			[FromBody] DeviceRegistrationForm form
		) {
			await notificationService.RegisterPushDevice(
				userAccountId: User.GetUserAccountId(),
				installationId: form.InstallationId,
				name: form.Name,
				token: form.Token
			);
			return Ok();
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
	}
}