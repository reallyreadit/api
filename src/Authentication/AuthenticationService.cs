using System;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Configuration;
using api.DataAccess.Models;
using api.Notifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace api.Authentication {
	public class AuthenticationService {
		private readonly DatabaseOptions databaseOptions;
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly NotificationService notificationService;
		public AuthenticationService(
			IOptions<DatabaseOptions> databaseOptions,
			IHttpContextAccessor httpContextAccessor,
			NotificationService notificationService
		) {
			this.databaseOptions = databaseOptions.Value;
			this.httpContextAccessor = httpContextAccessor;
			this.notificationService = notificationService;
		}
		public async Task SignIn(
			UserAccount user,
			PushDeviceForm pushDeviceForm	
		) {
			// create the authentication cookie
			var principal = new ClaimsPrincipal(
				new[] {
					new ClaimsIdentity(
						claims: new[] {
							new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
							new Claim(ClaimTypes.Role, user.Role.ToString())
						},
						authenticationType: "ApplicationCookie"
					)
				}
			);
			await httpContextAccessor.HttpContext.SignInAsync(
				principal: principal,
				properties: new AuthenticationProperties() {
					IsPersistent = true
				}
			);
			httpContextAccessor.HttpContext.User = principal;
			// register the push device
			if (pushDeviceForm?.IsValid() ?? false) {
				await notificationService.RegisterPushDevice(
					userAccountId: user.Id,
					installationId: pushDeviceForm.InstallationId,
					name: pushDeviceForm.Name,
					token: pushDeviceForm.Token
				);
			}
		}
		public async Task SignOut(
			string pushDeviceInstallationId
		) {
			// unregister the push device
			if (
				httpContextAccessor.HttpContext.User.Identity.IsAuthenticated &&
				!String.IsNullOrWhiteSpace(pushDeviceInstallationId)
			) {
				await notificationService.UnregisterPushDevice(
					installationId: pushDeviceInstallationId,
					reason: NotificationPushUnregistrationReason.SignOut
				);
			}
			// clear the authentication cookie
			await this.httpContextAccessor.HttpContext.SignOutAsync();
		}
	}
}