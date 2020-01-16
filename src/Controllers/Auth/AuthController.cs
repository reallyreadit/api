using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using api.Analytics;
using api.Authentication;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.Auth {
	public class AuthController : Controller {
		private string GetErrorMessage(AppleIdCredentialAuthenticationError? error) {
			switch (error) {
				case AppleIdCredentialAuthenticationError.InvalidIdToken:
					return "AppleIdInvalidJwt";
				case AppleIdCredentialAuthenticationError.InvalidSessionId:
					return "AppleIdInvalidSession";
				default:
					return "AppleIdUnknownError";
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppleIos(
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts,
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromServices] AppleAuthService appleAuthService,
			[FromServices] AuthenticationService authService,
			[FromBody] AppleIdCredentialAuthForm form
		) {
			var (authenticationId, user, error) = await appleAuthService.AuthenticateAppleIdCredential(
				sessionId: HttpContext.GetSessionId(),
				rawIdToken: form.IdentityToken,
				authCode: form.AuthorizationCode,
				emailAddress: form.Email,
				realUserRating: form.RealUserStatus,
				analytics: new UserAccountCreationAnalytics(
					client: this.GetClientAnalytics(),
					marketingVariant: form.Analytics.MarketingVariant,
					referrerUrl: form.Analytics.ReferrerUrl,
					initialPath: form.Analytics.InitialPath,
					currentPath: form.Analytics.CurrentPath,
					action: form.Analytics.Action
				),
				client: AppleClient.Ios
			);
			if (authenticationId.HasValue) {
				return Json(
					new {
						AuthServiceToken = Encryption.StringEncryption.Encrypt(
							authenticationId.ToString(),
							tokenizationOptions.Value.EncryptionKey
						)
					}
				);
			}
			if (user != null) {
				await authService.SignIn(user, form.PushDevice);
				return Json(
					new {
						User = user
					}
				);
			}
			return BadRequest(new [] { GetErrorMessage(error) });
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppleWeb(
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts,
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromServices] AppleAuthService appleAuthService,
			[FromServices] AuthenticationService authService,
			[FromForm] AppleWebForm form
		) {
			// check if the user cancelled the authentication
			if (!String.IsNullOrWhiteSpace(form.Error)) {
				return Redirect(serviceOpts.Value.WebServer.CreateUrl(form.State.CurrentPath));
			}
			var (authenticationId, user, error) = await appleAuthService.AuthenticateAppleIdCredential(
				sessionId: HttpContext.GetSessionId(),
				rawIdToken: form.IdToken,
				authCode: form.Code,
				emailAddress: form.User?.Email,
				realUserRating: null,
				analytics: new UserAccountCreationAnalytics(
					client: ClientAnalytics.ParseClientString(form.State.Client),
					marketingVariant: form.State.MarketingVariant,
					referrerUrl: form.State.ReferrerUrl,
					initialPath: form.State.InitialPath,
					currentPath: form.State.CurrentPath,
					action: form.State.Action
				),
				client: AppleClient.Web
			);
			if (authenticationId.HasValue) {
				return Redirect(
					serviceOpts.Value.WebServer.CreateUrl(form.State.CurrentPath) +
					"?authServiceToken=" +
					WebUtility.UrlEncode(
						Encryption.StringEncryption.Encrypt(
							authenticationId.ToString(),
							tokenizationOptions.Value.EncryptionKey
						)
					)
				);
			}
			if (user != null) {
				await authService.SignIn(user, PushDeviceForm.Blank);
				return Redirect(serviceOpts.Value.WebServer.CreateUrl(form.State.CurrentPath));
			}
			return Redirect(serviceOpts.Value.WebServer.CreateUrl(form.State.CurrentPath) + "?message=" + GetErrorMessage(error));
		}
	}
}