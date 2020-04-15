using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using api.Analytics;
using api.Authentication;
using api.Configuration;
using api.DataAccess.Models;
using api.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace api.Controllers.Auth {
	public class AuthController : Controller {
		private readonly ServiceEndpointsOptions serviceOpts;
		private readonly TokenizationOptions tokenizationOptions;
		public AuthController(
			IOptions<ServiceEndpointsOptions> serviceOpts,
			IOptions<TokenizationOptions> tokenizationOptions
		) {
			this.serviceOpts = serviceOpts.Value;
			this.tokenizationOptions = tokenizationOptions.Value;
		}
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
		private string GetErrorMessage(TwitterTokenAuthenticationError? error) {
			switch (error) {
				case TwitterTokenAuthenticationError.EmailAddressRequired:
					return "TwitterEmailAddressRequired";
				case TwitterTokenAuthenticationError.VerificationFailed:
					return "TwitterVerificationFailed";
				default:
					return "TwitterUnknownError";
			}
		}
		private RedirectResult RedirectToWebServer(string path, IEnumerable<KeyValuePair<string, string>> query = null) => (
			Redirect(serviceOpts.WebServer.CreateUrl(path, query))
		);
		private RedirectResult RedirectWithAuthToken(string path, long authenticationId) => (
			RedirectToWebServer(
				path,
				new[] {
					new KeyValuePair<string, string>(
						"authServiceToken",
						Encryption.StringEncryption.Encrypt(
							authenticationId.ToString(),
							tokenizationOptions.EncryptionKey
						)	
					)
				}
			)
		);
		private RedirectResult RedirectWithError(string path, string message) => (
			RedirectToWebServer(
				path,
				new [] {
					new KeyValuePair<string, string>("message", message)
				}
			)
		);
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppleIos(
			[FromServices] AppleAuthService appleAuthService,
			[FromServices] AuthenticationService authService,
			[FromBody] AppleIdCredentialAuthForm form
		) {
			var (authenticationId, user, error) = await appleAuthService.AuthenticateAppleIdCredential(
				sessionId: HttpContext.GetSessionId(),
				rawIdToken: form.IdentityToken,
				authCode: form.AuthorizationCode,
				emailAddress: form.Email,
				appleRealUserRating: form.RealUserStatus,
				signUpAnalytics: new UserAccountCreationAnalytics(
					client: this.GetClientAnalytics(),
					form: form.Analytics
				),
				client: AppleClient.Ios
			);
			if (authenticationId.HasValue) {
				return Json(
					new {
						AuthServiceToken = Encryption.StringEncryption.Encrypt(
							authenticationId.ToString(),
							tokenizationOptions.EncryptionKey
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
			[FromServices] AppleAuthService appleAuthService,
			[FromServices] AuthenticationService authService,
			[FromForm] AppleWebForm form
		) {
			// check if the user cancelled the authentication
			if (!String.IsNullOrWhiteSpace(form.Error)) {
				return RedirectToWebServer(form.State.CurrentPath);
			}
			var (authenticationId, user, error) = await appleAuthService.AuthenticateAppleIdCredential(
				sessionId: HttpContext.GetSessionId(),
				rawIdToken: form.IdToken,
				authCode: form.Code,
				emailAddress: form.User?.Email,
				appleRealUserRating: null,
				signUpAnalytics: new UserAccountCreationAnalytics(
					client: ClientAnalytics.ParseClientString(form.State.Client),
					form: form.State
				),
				client: AppleClient.Web
			);
			if (authenticationId.HasValue) {
				return RedirectWithAuthToken(form.State.CurrentPath, authenticationId.Value);
			}
			if (user != null) {
				await authService.SignIn(user, PushDeviceForm.Blank);
				return RedirectToWebServer(form.State.CurrentPath);
			}
			return RedirectWithError(form.State.CurrentPath, GetErrorMessage(error));
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> TwitterAuthentication(
			[FromServices] AuthenticationService authService,
			[FromServices] TwitterAuthService twitterAuth,
			[FromBody] TwitterCredentialAuthForm form
		) {
			var (authServiceAccount, authentication, user, error) = await twitterAuth.Authenticate(
				sessionId: HttpContext.GetSessionId(),
				requestTokenValue: form.OAuthToken,
				requestVerifier: form.OAuthVerifier,
				signUpAnalytics: new UserAccountCreationAnalytics(
					client: this.GetClientAnalytics(),
					form: form.Analytics
				)
			);
			if (
				authServiceAccount != null &&
				!authServiceAccount.IsPostIntegrationEnabled &&
				form.Integrations == AuthServiceIntegration.Post
			) {
				await twitterAuth.SetIntegrationPreference(
					identityId: authServiceAccount.IdentityId,
					integrations: form.Integrations
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
			if (authentication != null) {
				return Json(
					new {
						AuthServiceToken = Encryption.StringEncryption.Encrypt(
							authentication.Id.ToString(),
							tokenizationOptions.EncryptionKey
						)
					}
				);
			}
			return BadRequest(
				new[] {
					GetErrorMessage(error)
				}
			);
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> TwitterBrowserRequest(
			[FromServices] TwitterAuthService twitterAuth,
			[FromBody] TwitterBrowserRequestForm form
		) {
			var token = await twitterAuth.GetBrowserRequestToken(
				redirectPath: form.RedirectPath,
				integrations: form.Integrations,
				signUpAnalytics: form.SignUpAnalytics != null ?
					new UserAccountCreationAnalytics(
						client: this.GetClientAnalytics(),
						form: form.SignUpAnalytics
					) :
					null
			);
			if (token != null) {
				return Json(
					new {
						Value = token.OAuthToken
					}
				);
			}
			return BadRequest();
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> TwitterBrowserVerification(
			[FromServices] AuthenticationService authService,
			[FromServices] TwitterAuthService twitterAuth,
			[FromQuery] TwitterBrowserVerificationForm form
		) {
			// check if the user cancelled the authentication
			if (!String.IsNullOrWhiteSpace(form.Denied)) {
				await twitterAuth.CancelRequest(form.Denied);
				return RedirectToWebServer(form.ReadupRedirectPath);
			}
			if (User.Identity.IsAuthenticated) {
				var (authServiceAccount, error) = await twitterAuth.LinkAccount(
					sessionId: HttpContext.GetSessionId(),
					requestTokenValue: form.OAuthToken,
					requestVerifier: form.OAuthVerifier,
					userAccountId: User.GetUserAccountId()
				);
				if (authServiceAccount != null) {
					await twitterAuth.SetIntegrationPreference(
						identityId: authServiceAccount.IdentityId,
						integrations: form.ReadupIntegrations
					);
					return RedirectToWebServer(form.ReadupRedirectPath);
				}
				return RedirectWithError(form.ReadupRedirectPath, GetErrorMessage(error));
			} else {
				var (authServiceAccount, authentication, user, error) = await twitterAuth.Authenticate(
					sessionId: HttpContext.GetSessionId(),
					requestTokenValue: form.OAuthToken,
					requestVerifier: form.OAuthVerifier,
					signUpAnalytics: null
				);
				if (
					authServiceAccount != null &&
					!authServiceAccount.IsPostIntegrationEnabled &&
					form.ReadupIntegrations == AuthServiceIntegration.Post
				) {
					await twitterAuth.SetIntegrationPreference(
						identityId: authServiceAccount.IdentityId,
						integrations: form.ReadupIntegrations
					);
				}
				if (user != null) {
					await authService.SignIn(user, PushDeviceForm.Blank);
					return RedirectToWebServer(form.ReadupRedirectPath);
				}
				if (authentication != null) {
					return RedirectWithAuthToken(form.ReadupRedirectPath, authentication.Id);
				}
				return RedirectWithError(form.ReadupRedirectPath, GetErrorMessage(error));
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> TwitterWebViewRequest(
			[FromServices] TwitterAuthService twitterAuth
		) {
			var token = await twitterAuth.GetWebViewRequestToken();
			if (token != null) {
				return Json(
					new {
						Value = token.OAuthToken
					}
				);
			}
			return BadRequest();
		}
		[HttpPost]
		public async Task<IActionResult> TwitterLink(
			[FromServices] AuthenticationService authService,
			[FromServices] TwitterAuthService twitterAuth,
			[FromBody] TwitterCredentialLinkForm form
		) {
			var (authServiceAccount, error) = await twitterAuth.LinkAccount(
				sessionId: HttpContext.GetSessionId(),
				requestTokenValue: form.OAuthToken,
				requestVerifier: form.OAuthVerifier,
				userAccountId: User.GetUserAccountId()
			);
			if (authServiceAccount != null) {
				authServiceAccount = await twitterAuth.SetIntegrationPreference(
					identityId: authServiceAccount.IdentityId,
					integrations: form.Integrations
				);
				return Json(
					new AuthServiceAccountAssociation(authServiceAccount)
				);
			}
			return BadRequest(
				new[] {
					GetErrorMessage(error)
				}
			);
		}
	}
}