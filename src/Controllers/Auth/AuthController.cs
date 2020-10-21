using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using api.Analytics;
using api.Authentication;
using api.Configuration;
using api.Cookies;
using api.DataAccess;
using api.DataAccess.Models;
using api.Models;
using api.Notifications;
using api.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.Auth {
	public class AuthController : Controller {
		private readonly AuthenticationOptions authenticationOptions;
		private readonly JsonSerializerOptions browserPopupCookieSerializationOptions = new JsonSerializerOptions() {
			IgnoreNullValues = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
		private readonly CookieOptions cookieOptions;
		private readonly ServiceEndpointsOptions serviceOpts;
		private readonly TokenizationOptions tokenizationOptions;
		public AuthController(
			IOptions<AuthenticationOptions> authenticationOptions,
			IOptions<CookieOptions> cookieOptions,
			IOptions<ServiceEndpointsOptions> serviceOpts,
			IOptions<TokenizationOptions> tokenizationOptions
		) {
			this.authenticationOptions = authenticationOptions.Value;
			this.cookieOptions = cookieOptions.Value;
			this.serviceOpts = serviceOpts.Value;
			this.tokenizationOptions = tokenizationOptions.Value;
		}
		private string CreateAuthServiceToken(long authenticationId) => Encryption.StringEncryption.Encrypt(
			authenticationId.ToString(),
			tokenizationOptions.EncryptionKey
		);
		private string GetErrorMessage(AuthServiceProvider provider, AuthenticationError? error) {
			string message;
			switch (provider) {
				case AuthServiceProvider.Apple:
					message = "Apple";
					break;
				case AuthServiceProvider.Twitter:
					message = "Twitter";
					break;
				default:
					message = "UnknownProvider";
					break;
			}
			switch (error) {
				case AuthenticationError.Cancelled:
					message += "Cancelled";
					break;
				case AuthenticationError.InvalidAuthToken:
					message += "InvalidAuthToken";
					break;
				case AuthenticationError.InvalidSessionId:
					message += "InvalidSessionId";
					break;
				case AuthenticationError.EmailAddressRequired:
					message += "EmailAddressRequired";
					break;
				default:
					message = "UnknownError";
					break;
			}
			return message;
		}
		private RedirectResult RedirectToAuthServiceLinkHandler<T>(T response) where T : AuthServiceBrowserLinkResponse => (
			RedirectToWebServer(
				"/auth-service-link-handler/index.html",
				new[] {
					new KeyValuePair<string, string>(
						key: "body",
						value: JsonSerializer.Serialize(
							response,
							new JsonSerializerOptions() {
								PropertyNamingPolicy = JsonNamingPolicy.CamelCase
							}
						)
					)
				}
			)
		);
		private RedirectResult RedirectToBrowserPopupHandler(string requestId, BrowserPopupCookie cookie) {
			Response.Cookies.SetAuthServiceBrowserPopupCookie(
				requestId: requestId,
				value: JsonSerializer.Serialize(cookie, browserPopupCookieSerializationOptions),
				apiServerHost: serviceOpts.ApiServer.Host
			);
			return Redirect(
				serviceOpts.StaticContentServer.CreateUrl(
					"/common/auth-service-popup-handler/v1/index.html"
				)
			);
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
						CreateAuthServiceToken(authenticationId)
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
			[FromServices] IOptions<DatabaseOptions> databaseOptions,
			[FromBody] AppleIdCredentialAuthForm form
		) {
			var (authenticationId, user, error) = await appleAuthService.AuthenticateAppleIdCredential(
				sessionId: Request.Cookies.GetSessionIdCookieValue(),
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
						AuthServiceToken = CreateAuthServiceToken(authenticationId.Value)
					}
				);
			}
			if (user != null) {
				await authService.SignIn(user, form.PushDevice);
				using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
					return Json(
						new {
							User = user,
							DisplayPreference = await db.GetDisplayPreference(user.Id)
						}
					);
				}
			}
			return BadRequest(new [] { GetErrorMessage(AuthServiceProvider.Apple, error) });
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppleWeb(
			[FromServices] AppleAuthService appleAuthService,
			[FromServices] AuthenticationService authService,
			[FromForm] AppleWebRedirectForm form
		) {
			// check if the user cancelled the authentication
			if (!String.IsNullOrWhiteSpace(form.Error)) {
				return RedirectToWebServer(form.State.CurrentPath);
			}
			var (authenticationId, user, error) = await appleAuthService.AuthenticateAppleIdCredential(
				sessionId: Request.Cookies.GetSessionIdCookieValue(),
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
			return RedirectWithError(form.State.CurrentPath, GetErrorMessage(AuthServiceProvider.Apple, error));
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppleWebPopup(
			[FromServices] AppleAuthService appleAuthService,
			[FromServices] AuthenticationService authService,
			[FromForm] AppleWebPopupForm form
		) {
			// we're going to redirect to the popup handler no matter what
			BrowserPopupCookie cookie;
			// check if the user cancelled the authentication
			if (!String.IsNullOrWhiteSpace(form.Error)) {
				cookie = new BrowserPopupCookie() {
					Error = AuthenticationError.Cancelled
				};
			// check if the user is linking an existing account
			} else if (User.Identity.IsAuthenticated) {	
				var (authServiceAccount, error) = await appleAuthService.LinkAccount(
					sessionId: Request.Cookies.GetSessionIdCookieValue(),
					rawIdToken: form.IdToken,
					authCode: form.Code,
					emailAddress: form.User?.Email,
					appleRealUserRating: null,
					signUpAnalytics: null,
					client: AppleClient.Web,
					userAccountId: User.GetUserAccountId()
				);
				// check if the linking was successful
				if (authServiceAccount != null) {
					cookie = new BrowserPopupCookie() {
						IdentityId = authServiceAccount.IdentityId
					};
				} else {
					cookie = new BrowserPopupCookie() {
						Error = error ?? AuthenticationError.Unknown
					};
				}
			// attempt to authenticate
			} else {
				var (authenticationId, user, error) = await appleAuthService.AuthenticateAppleIdCredential(
					sessionId: Request.Cookies.GetSessionIdCookieValue(),
					rawIdToken: form.IdToken,
					authCode: form.Code,
					emailAddress: form.User?.Email,
					appleRealUserRating: null,
					signUpAnalytics: null,
					client: AppleClient.Web
				);
				// check if we found an existing account
				if (user != null) {
					await authService.SignIn(user, PushDeviceForm.Blank);
					cookie = new BrowserPopupCookie();
				// check if we have an auth service token to return
				} else if (authenticationId.HasValue) {
					cookie = new BrowserPopupCookie() {
						Token = CreateAuthServiceToken(authenticationId.Value)
					};
				} else {
					cookie = new BrowserPopupCookie() {
						Error = error ?? AuthenticationError.Unknown
					};
				}
			}
			return RedirectToBrowserPopupHandler(
				requestId: form.State.RequestId,
				cookie: cookie
			);
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> BrowserPopupRequest(
			[FromServices] TwitterAuthService twitterAuth,
			[FromBody] BrowserPopupRequestRequest request
		) {
			var requestId = Guid
				.NewGuid()
				.ToString();
			BrowserPopupRequestResponse response;
			switch (request.Provider) {
				case AuthServiceProvider.Apple:
					response = new BrowserPopupRequestResponse(
						requestId: requestId,
						popupUrl: (
							authenticationOptions.AppleAuth.WebAuthUrl +
							QueryStringSerializer.Serialize(
								query: new[] {
									new KeyValuePair<string, string>("client_id", "com.readup.webapp"),
									new KeyValuePair<string, string>("redirect_uri", authenticationOptions.AppleAuth.WebAuthPopupRedirectUrl),
									new KeyValuePair<string, string>("response_type", "code id_token"),
									new KeyValuePair<string, string>("scope", "email"),
									new KeyValuePair<string, string>("response_mode", "form_post"),
									new KeyValuePair<string, string>(
										"state",
										 JsonSerializer.Serialize(
											new AppleWebPopupState() {
												RequestId = requestId
											},
											new JsonSerializerOptions() {
												PropertyNamingPolicy = JsonNamingPolicy.CamelCase
											}
										)
									)
								},
								includePrefix: true
							)
						)
					);
					break;
				case AuthServiceProvider.Twitter:
					var token = await twitterAuth.GetBrowserPopupRequestToken(
						requestId: requestId
					);
					if (token == null) {
						return Problem("Failed to retrieve request token");
					}
					response = new BrowserPopupRequestResponse(
						requestId: requestId,
						popupUrl: (
							authenticationOptions.TwitterAuth.TwitterApiServerUrl +
							"/oauth/authorize" +
							QueryStringSerializer.Serialize(
								query: new [] {
									new KeyValuePair<string, string>("oauth_token", token.OAuthToken)
								},
								includePrefix: true
							)
						)
					);
					break;
				default:
					return Problem($"Unexpected value for {nameof(request.Provider)}", statusCode: 400);
			}
			return Json(response);
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> BrowserPopupResponse(
			[FromServices] IOptions<DatabaseOptions> databaseOptions,
			[FromQuery] BrowserPopupResponseRequest request
		) {
			var serializedCookieValue = Request.Cookies.GetAuthServiceBrowserPopupCookieValue(request.RequestId);
			if (serializedCookieValue == null) {
				return Problem(statusCode: 404);
			}
			var cookie = JsonSerializer.Deserialize<BrowserPopupCookie>(serializedCookieValue, browserPopupCookieSerializationOptions);
			BrowserPopupResponseResponse response;
			if (cookie.Error.HasValue) {
				response = new BrowserPopupResponseResponse(cookie.Error.Value);
			} else if (
				!String.IsNullOrWhiteSpace(cookie.Token)
			) {
				response = new BrowserPopupResponseResponse(
					authServiceToken: cookie.Token
				);
			} else if (cookie.IdentityId.HasValue) {
				using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
					response = new BrowserPopupResponseResponse(
						new AuthServiceAccountAssociation(
							await db.GetAuthServiceAccountByIdentityId(cookie.IdentityId.Value)
						)
					);
				}
			} else if (User.Identity.IsAuthenticated) {
				// check if user is signed in last
				var userAccountId = User.GetUserAccountId();
				using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
					response = new BrowserPopupResponseResponse(
						new WebAppUserProfile(
							await db.GetDisplayPreference(userAccountId),
							await db.GetUserAccountById(userAccountId)
						)
					);
				}
			} else {
				response = new BrowserPopupResponseResponse(AuthenticationError.Unknown);	
			}
			return Json(response);
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> TwitterAuthentication(
			[FromServices] AuthenticationService authService,
			[FromServices] IOptions<DatabaseOptions> databaseOptions,
			[FromServices] TwitterAuthService twitterAuth,
			[FromBody] TwitterCredentialAuthForm form
		) {
			var (authServiceAccount, authentication, user, error) = await twitterAuth.Authenticate(
				sessionId: Request.Cookies.GetSessionIdCookieValue(),
				requestTokenValue: form.OAuthToken,
				requestVerifier: form.OAuthVerifier,
				signUpAnalytics: new UserAccountCreationAnalytics(
					client: this.GetClientAnalytics(),
					form: form.Analytics
				)
			);
			if (user != null) {
				await authService.SignIn(user, form.PushDevice);
				using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
					return Json(
						new {
							User = user,
							DisplayPreference = await db.GetDisplayPreference(user.Id)
						}
					);
				}
			}
			if (authentication != null) {
				return Json(
					new {
						AuthServiceToken = CreateAuthServiceToken(authentication.Id)
					}
				);
			}
			return BadRequest(
				new[] {
					GetErrorMessage(AuthServiceProvider.Twitter, error)
				}
			);
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> TwitterAuthenticationCallback(
			[FromServices] AuthenticationService authService,
			[FromServices] TwitterAuthService twitterAuth,
			[FromQuery] TwitterAuthenticationCallbackRequest request
		) {
			// check if the user cancelled the authentication
			if (!String.IsNullOrWhiteSpace(request.Denied)) {
				await twitterAuth.CancelRequest(request.OAuthToken);
				return RedirectToWebServer(request.ReadupRedirectPath);
			}
			var (authServiceAccount, authentication, user, error) = await twitterAuth.Authenticate(
				sessionId: Request.Cookies.GetSessionIdCookieValue(),
				requestTokenValue: request.OAuthToken,
				requestVerifier: request.OAuthVerifier,
				signUpAnalytics: null
			);
			if (user != null) {
				await authService.SignIn(user, PushDeviceForm.Blank);
				return RedirectToWebServer(request.ReadupRedirectPath);
			}
			if (authentication != null) {
				return RedirectWithAuthToken(request.ReadupRedirectPath, authentication.Id);
			}
			return RedirectWithError(request.ReadupRedirectPath, GetErrorMessage(AuthServiceProvider.Twitter, error));
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> TwitterBrowserAuthRequest(
			[FromServices] TwitterAuthService twitterAuth,
			[FromBody] TwitterBrowserAuthRequestTokenRequest request
		) {
			var token = await twitterAuth.GetBrowserAuthRequestToken(
				redirectPath: request.RedirectPath,
				signUpAnalytics: new UserAccountCreationAnalytics(
					client: this.GetClientAnalytics(),
					form: request.SignUpAnalytics
				)
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
		[HttpPost]
		public async Task<IActionResult> TwitterBrowserLinkRequest(
			[FromServices] TwitterAuthService twitterAuth
		) {
			var token = await twitterAuth.GetBrowserLinkRequestToken();
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
				sessionId: Request.Cookies.GetSessionIdCookieValue(),
				requestTokenValue: form.OAuthToken,
				requestVerifier: form.OAuthVerifier,
				userAccountId: User.GetUserAccountId()
			);
			if (authServiceAccount != null) {
				return Json(
					new AuthServiceAccountAssociation(authServiceAccount)
				);
			}
			return BadRequest(
				new[] {
					GetErrorMessage(AuthServiceProvider.Twitter, error)
				}
			);
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> TwitterLinkCallback(
			[FromServices] AuthenticationService authService,
			[FromServices] TwitterAuthService twitterAuth,
			[FromQuery] TwitterCallbackRequest request
		) {
			// check if the user cancelled the authentication
			if (!String.IsNullOrWhiteSpace(request.Denied)) {
				await twitterAuth.CancelRequest(request.OAuthToken);
				return RedirectToAuthServiceLinkHandler(
					new AuthServiceBrowserLinkFailureResponse(
						error: AuthenticationError.Cancelled,
						requestToken: request.OAuthToken
					)
				);
			}
			var (authServiceAccount, error) = await twitterAuth.LinkAccount(
				sessionId: Request.Cookies.GetSessionIdCookieValue(),
				requestTokenValue: request.OAuthToken,
				requestVerifier: request.OAuthVerifier,
				userAccountId: User.GetUserAccountId()
			);
			if (authServiceAccount != null) {
				return RedirectToAuthServiceLinkHandler(
					new AuthServiceBrowserLinkSuccessResponse(
						association: new AuthServiceAccountAssociation(authServiceAccount),
						requestToken: request.OAuthToken
					)
				);
			}
			return RedirectToAuthServiceLinkHandler(
				new AuthServiceBrowserLinkFailureResponse(
					error: error ?? AuthenticationError.InvalidAuthToken,
					requestToken: request.OAuthToken
				)
			);
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> TwitterPopupCallback(
			[FromServices] AuthenticationService authService,
			[FromServices] TwitterAuthService twitterAuth,
			[FromQuery] TwitterPopupCallbackRequest request
		) {
			// we're going to redirect to the popup handler no matter what
			BrowserPopupCookie cookie;
			// check if the user cancelled the authentication
			if (!String.IsNullOrWhiteSpace(request.Denied)) {
				await twitterAuth.CancelRequest(request.OAuthToken);
				cookie = new BrowserPopupCookie() {
					Error = AuthenticationError.Cancelled
				};
			// check if the user is linking an existing account
			} else if (User.Identity.IsAuthenticated) {	
				var (authServiceAccount, error) = await twitterAuth.LinkAccount(
					sessionId: Request.Cookies.GetSessionIdCookieValue(),
					requestTokenValue: request.OAuthToken,
					requestVerifier: request.OAuthVerifier,
					userAccountId: User.GetUserAccountId()
				);
				// check if the linking was successful
				if (authServiceAccount != null) {
					cookie = new BrowserPopupCookie() {
						IdentityId = authServiceAccount.IdentityId
					};
				} else {
					cookie = new BrowserPopupCookie() {
						Error = error ?? AuthenticationError.Unknown
					};
				}
			// attempt to authenticate
			} else {
				var (authServiceAccount, authentication, user, error) = await twitterAuth.Authenticate(
					sessionId: Request.Cookies.GetSessionIdCookieValue(),
					requestTokenValue: request.OAuthToken,
					requestVerifier: request.OAuthVerifier,
					signUpAnalytics: null
				);
				// check if we found an existing account
				if (user != null) {
					await authService.SignIn(user, PushDeviceForm.Blank);
					cookie = new BrowserPopupCookie();
				// check if we have an auth service token to return
				} else if (authentication != null) {
					cookie = new BrowserPopupCookie() {
						Token = CreateAuthServiceToken(authentication.Id)
					};
				} else {
					cookie = new BrowserPopupCookie() {
						Error = error ?? AuthenticationError.Unknown
					};
				}
			}
			return RedirectToBrowserPopupHandler(
				requestId: request.ReadupRequestId,
				cookie: cookie
			);
		}
	}
}