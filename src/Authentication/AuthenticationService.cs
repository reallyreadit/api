using System;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Configuration;
using api.Cookies;
using api.DataAccess;
using api.DataAccess.Models;
using api.Notifications;
using Microsoft.AspNetCore.Authentication;
using IHttpContextAccessor = Microsoft.AspNetCore.Http.IHttpContextAccessor;
using Microsoft.Extensions.Options;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace api.Authentication {
	public class AuthenticationService {
		private readonly CookieOptions cookieOptions;
		private readonly DatabaseOptions databaseOptions;
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly ILogger<AuthenticationService> logger;
		private readonly NotificationService notificationService;
		private readonly TokenizationOptions tokenizationOptions;
		public AuthenticationService(
			IOptions<CookieOptions> cookieOptions,
			IOptions<DatabaseOptions> databaseOptions,
			IHttpContextAccessor httpContextAccessor,
			ILogger<AuthenticationService> logger,
			NotificationService notificationService,
			IOptions<TokenizationOptions> tokenizationOptions
		) {
			this.cookieOptions = cookieOptions.Value;
			this.databaseOptions = databaseOptions.Value;
			this.httpContextAccessor = httpContextAccessor;
			this.logger = logger;
			this.notificationService = notificationService;
			this.tokenizationOptions = tokenizationOptions.Value;
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
			// merge provision account if present
			var provisionalUserAccountId = httpContextAccessor.HttpContext.Request.Cookies.GetProvisionalSessionKeyCookieValue(this.tokenizationOptions);
			if (provisionalUserAccountId.HasValue) {
				try {
					using (var dbConnection = new NpgsqlConnection(databaseOptions.ConnectionString)) {
						await dbConnection.MergeProvisionalUserAccount(
							provisionalUserAccountId: provisionalUserAccountId.Value,
							userAccountId: user.Id
						);
					}
				} catch (Exception exception) {
					if ((exception as PostgresException)?.SqlState == "RU001") {
						logger.LogError(
							exception,
							"Provisional user account has already been merged. Provisional user account id: {Id}",
							provisionalUserAccountId.Value.ToString()
						);
					} else {
						logger.LogError(
							exception,
							"Unexpected error occurred attempted to merge provisional user account. Provisional user account id: {Id}",
							provisionalUserAccountId.Value.ToString()
						);
					}
				}
				httpContextAccessor.HttpContext.Response.Cookies.ClearProvisionalSessionKeyCookie(cookieOptions);
			}
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