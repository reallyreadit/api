using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using api.DataAccess;
using System;
using System.Linq;
using System.Threading.Tasks;
using api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using api.Configuration;
using System.Collections.Generic;
using api.Encryption;
using api.DataAccess.Models;
using System.Net;
using api.Authorization;
using Npgsql;
using api.Security;
using api.Commenting;
using api.Analytics;
using api.Notifications;
using api.Routing;
using Microsoft.Extensions.Logging;
using api.Controllers.Shared;
using api.Cookies;

namespace api.Controllers.UserAccounts {
	public class UserAccountsController : Controller {
		private readonly DatabaseOptions dbOpts;
		private readonly TokenizationOptions tokenOpts;
		private readonly ILogger<UserAccountsController> log;
		public UserAccountsController(
			IOptions<DatabaseOptions> dbOpts,
			IOptions<TokenizationOptions> tokenOpts,
			ILogger<UserAccountsController> log
		) {
			this.dbOpts = dbOpts.Value;
			this.tokenOpts = tokenOpts.Value;
			this.log = log;
		}
		private static byte[] GenerateSalt() {
			var salt = new byte[128 / 8];
			using (var rng = RandomNumberGenerator.Create()) {
				rng.GetBytes(salt);
			}
			return salt;
		}
		private static byte[] HashPassword(string password, byte[] salt) => KeyDerivation.Pbkdf2(
			password: password,
			salt: salt,
			prf: KeyDerivationPrf.HMACSHA1,
			iterationCount: 10000,
			numBytesRequested: 256 / 8
		);
		private static long GetTimeZoneIdFromName(IEnumerable<api.DataAccess.Models.TimeZone> timeZones, string timeZoneName) {
			var timeZone = timeZones
				.Where(zone => zone.Name == timeZoneName)
				.OrderBy(zone => zone.Territory)
				.FirstOrDefault();
			if (timeZone == null) {
				// default to UTC if no match is found
				// UTC is an official alias for Etc/UTC but we'll also use it as a catch-all instead of throwing an error
				timeZone = timeZones.Single(
					zone => zone.Name == "Etc/UTC"
				);
			}
			return timeZone.Id;
		}
		// deprecated
		private IActionResult ReadReplyAndRedirectToArticle(Comment reply, RoutingService routingService) {
			return Redirect(routingService.CreateCommentUrl(reply.ArticleSlug, reply.Id).ToString());
		}
		private bool IsPasswordValid(string password) =>
			!String.IsNullOrWhiteSpace(password) &&
			password.Length >= 8 &&
			password.Length <= 256;
		private bool IsCorrectPassword(UserAccount userAccount, string password) => (
			userAccount.IsPasswordSet &&
			userAccount.PasswordHash.SequenceEqual(HashPassword(password, userAccount.PasswordSalt))
		);
		private bool IsEmailConfirmationRateExceeded(EmailConfirmation latestUnconfirmedConfirmation) =>
			latestUnconfirmedConfirmation != null ?
				DateTime.UtcNow.Subtract(latestUnconfirmedConfirmation.DateCreated).TotalMinutes < 5 :
				false;
		private bool IsPasswordResetRequestValid(PasswordResetRequest request) =>
				request != null &&
				!request.DateCompleted.HasValue &&
				DateTime.UtcNow.Subtract(request.DateCreated).TotalHours < 24;
		[AllowAnonymous]
		[HttpPost]
      public async Task<IActionResult> CreateAccount(
			[FromServices] AuthenticationService authenticationService,
			[FromServices] CaptchaService captchaService,
			[FromServices] NotificationService notificationService,
			[FromBody] UserAccountForm form
		) {
			if (!IsPasswordValid(form.Password)) {
				return BadRequest();
			}
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var captchaResponse = await captchaService.Verify(form.CaptchaResponse);
				if (captchaResponse != null) {
					db.CreateCaptchaResponse("createUserAccount", captchaResponse);
				}
				try {
					var salt = GenerateSalt();
					var userAccount = await db.CreateUserAccount(
						name: form.Name,
						email: form.Email,
						passwordHash: HashPassword(form.Password, salt),
						passwordSalt: salt,
						timeZoneId: GetTimeZoneIdFromName(db.GetTimeZones(), form.TimeZoneName),
						theme: form.Theme,
						analytics: new UserAccountCreationAnalytics(
							client: this.GetClientAnalytics(),
							form: form.Analytics
						)
					);
					await notificationService.CreateWelcomeNotification(userAccount.Id);
					await authenticationService.SignIn(userAccount, form.PushDevice);
					return Json(
						new WebAppUserProfileClientModel(
							await db.GetDisplayPreference(userAccount.Id),
							await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccount.Id),
							userAccount
						)
					);
				} catch (Exception ex) {
					return BadRequest((ex as ValidationException)?.Errors);
				}
			}
      }
		private long ParseAuthServiceToken(string rawValue) => (
			Int64.Parse(
				StringEncryption.Decrypt(
					text: rawValue,
					key: tokenOpts.EncryptionKey
				)
			)
		);
		private string ValidateAuthServiceAuthenticationForAssociation(
			AuthServiceAuthentication authentication
		) {
			if (Request.Cookies.GetSessionIdCookieValue() != authentication.SessionId) {
				return "InvalidSessionId";
			}
			if (DateTime.UtcNow.Subtract(authentication.DateAuthenticated) > TimeSpan.FromMinutes(5)) {
				return "AuthenticationExpired";
			}
			return null;
		}
		[AllowAnonymous]
		[HttpPost]
      public async Task<IActionResult> AuthServiceAccount(
			[FromServices] AuthenticationService authenticationService,
			[FromServices] NotificationService notificationService,
			[FromBody] AuthServiceAccountForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var authentication = await db.GetAuthServiceAuthenticationById(
					ParseAuthServiceToken(form.Token)
				);
				var validationError = ValidateAuthServiceAuthenticationForAssociation(authentication);
				if (validationError != null) {
					return BadRequest(new [] { validationError });
				}
				var authServiceAccount = await db.GetAuthServiceAccountByIdentityId(authentication.IdentityId);
				try {
					UserAccount userAccount;
					var timeZoneId = GetTimeZoneIdFromName(db.GetTimeZones(), form.TimeZoneName);
					if (
						!String.IsNullOrWhiteSpace(authServiceAccount.IdentitySignUpAnalytics)
					) {
						userAccount = await db.CreateUserAccount(
							name: form.Name,
							email: authServiceAccount.ProviderUserEmailAddress,
							passwordHash: null,
							passwordSalt: null,
							timeZoneId: timeZoneId,
							theme: form.Theme,
							analytics: authServiceAccount.IdentitySignUpAnalytics
						);
					} else {
						userAccount = await db.CreateUserAccount(
							name: form.Name,
							email: authServiceAccount.ProviderUserEmailAddress,
							passwordHash: null,
							passwordSalt: null,
							timeZoneId: timeZoneId,
							theme: form.Theme,
							analytics: new UserAccountCreationAnalytics(
								client: this.GetClientAnalytics(),
								form: form.Analytics
							)
						);
					}
					await db.AssociateAuthServiceAccount(
						identityId: authServiceAccount.IdentityId,
						authenticationId: authentication.Id,
						userAccountId: userAccount.Id,
						associationMethod: AuthServiceAssociationMethod.Manual
					);
					if (authServiceAccount.Provider == AuthServiceProvider.Twitter) {
						userAccount.HasLinkedTwitterAccount = true;
					}
					await notificationService.CreateWelcomeNotification(userAccount.Id);
					await authenticationService.SignIn(userAccount, form.PushDevice);
					return Json(
						new WebAppUserProfileClientModel(
							await db.GetDisplayPreference(userAccount.Id),
							await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccount.Id),
							userAccount
						)
					);
				} catch (Exception ex) {
					return BadRequest((ex as ValidationException)?.Errors);
				}
			}
      }
		// deprecated
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ConfirmEmail(
			string token,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var emailConfirmationId = Int64.Parse(StringEncryption.Decrypt(token, tokenOpts.EncryptionKey));
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var confirmation = db.GetEmailConfirmation(emailConfirmationId);
				var resultBaseUrl = serviceOpts.Value.WebServer.CreateUrl("/email/confirm");
				if (confirmation == null) {
					return Redirect(resultBaseUrl + "/not-found");
				}
				if (confirmation.DateConfirmed.HasValue) {
					return Redirect(resultBaseUrl + "/already-confirmed");
				}
				if (confirmation.Id != db.GetLatestUnconfirmedEmailConfirmation(confirmation.UserAccountId).Id) {
					return Redirect(resultBaseUrl + "/expired");
				}
				db.ConfirmEmailAddress(emailConfirmationId);
				return Redirect(resultBaseUrl + "/success");
			}
		}
		[HttpPost]
		public async Task<IActionResult> ResendConfirmationEmail(
			[FromServices] NotificationService notificationService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				if (IsEmailConfirmationRateExceeded(db.GetLatestUnconfirmedEmailConfirmation(this.User.GetUserAccountId()))) {
					return BadRequest(new[] { "ResendLimitExceeded" });
				}
				await notificationService.CreateEmailConfirmationNotification(this.User.GetUserAccountId());
			}
			return Ok();
		}
		[HttpPost]
		public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordBinder binder) {
			if (!IsPasswordValid(binder.NewPassword)) {
				return BadRequest();
			}
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccount = await db.GetUserAccountById(this.User.GetUserAccountId());
				if (!IsCorrectPassword(userAccount, binder.CurrentPassword)) {
					return BadRequest(new[] { "IncorrectPassword" });
				}
				var salt = GenerateSalt();
				db.ChangePassword(userAccount.Id, HashPassword(binder.NewPassword, salt), salt);
			}
			return Ok();
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> ResetPassword(
			[FromServices] AuthenticationService authenticationService,
			[FromBody] PasswordResetForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(form.Token, tokenOpts.EncryptionKey)));
				if (request == null) {
					return BadRequest(new[] { "RequestNotFound" });
				}
				if (IsPasswordResetRequestValid(request)) {
					if (!IsPasswordValid(form.Password)) {
						return BadRequest();
					}
					var salt = GenerateSalt();
					db.ChangePassword(request.UserAccountId, HashPassword(form.Password, salt), salt);
					db.CompletePasswordResetRequest(request.Id);
					if (request.AuthServiceAuthenticationId.HasValue) {
						var authentication = await db.GetAuthServiceAuthenticationById(request.AuthServiceAuthenticationId.Value);
						try {
							await db.AssociateAuthServiceAccount(
								identityId: authentication.IdentityId,
								authenticationId: authentication.Id,
								userAccountId: request.UserAccountId,
								associationMethod: AuthServiceAssociationMethod.Manual
							);
						} catch (NpgsqlException ex) when (
								ex.Data.Contains("ConstraintName") &&
								String.Equals(ex.Data["ConstraintName"], "auth_service_association_unique_associated_identity_id")
							) {
								// another association was completed before this password reset
						}
					}
					var userAccount = await db.GetUserAccountById(request.UserAccountId);
					await authenticationService.SignIn(userAccount, form.PushDevice);
					return Json(
						new WebAppUserProfileClientModel(
							await db.GetDisplayPreference(userAccount.Id),
							await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccount.Id),
							userAccount
						)
					);
				}
			}
			return BadRequest(new[] { "RequestExpired" });
		}
		[HttpPost]
		public async Task<IActionResult> ChangeEmailAddress(
			[FromBody] EmailAddressBinder binder,
			[FromServices] NotificationService notificationService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccount = await db.GetUserAccountById(this.User.GetUserAccountId());
				if (userAccount.Email == binder.Email) {
					return BadRequest();
				}
				var isEmailAddressConfirmed = db.IsEmailAddressConfirmed(userAccount.Id, binder.Email);
				var confirmations = db.ListEmailConfirmations(userAccount.Id);
				if (
					isEmailAddressConfirmed ||
					!confirmations.Any(confirmation => (
						confirmation.EmailAddress == binder.Email &&
						!confirmation.DateConfirmed.HasValue &&
						IsEmailConfirmationRateExceeded(confirmation))
					)
				) {
					try {
						db.ChangeEmailAddress(userAccount.Id, binder.Email);
					} catch (Exception ex) {
						return BadRequest((ex as ValidationException)?.Errors);
					}
					var confirmation = db.CreateEmailConfirmation(userAccount.Id);
					if (isEmailAddressConfirmed) {
						db.ConfirmEmailAddress(confirmation.Id);
					}
					var updatedUserAccount = await db.GetUserAccountById(userAccount.Id);
					if (!isEmailAddressConfirmed) {
						await notificationService.CreateEmailConfirmationNotification(userAccount.Id, confirmation.Id);
					}
					return Json(updatedUserAccount);
				}
			}
			return BadRequest(new[] { "ResendLimitExceeded" });
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> RequestPasswordReset(
			[FromBody] PasswordResetRequestForm form,
			[FromServices] NotificationService notificationService,
			[FromServices] CaptchaService captchaService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var captchaResponse = await captchaService.Verify(form.CaptchaResponse);
				if (captchaResponse != null) {
					db.CreateCaptchaResponse("requestPasswordReset", captchaResponse);
				}
				var userAccount = db.GetUserAccountByEmail(form.Email);
				if (userAccount == null) {
					return BadRequest(new[] { "UserAccountNotFound" });
				}
				var latestRequest = db.GetLatestPasswordResetRequest(userAccount.Id);
				if (IsPasswordResetRequestValid(latestRequest)) {
					return BadRequest(new[] { "RequestLimitExceeded" });
				}
				var request = await db.CreatePasswordResetRequest(
					userAccount.Id,
					!String.IsNullOrWhiteSpace(form.AuthServiceToken) ?
						new Nullable<Int64>(ParseAuthServiceToken(form.AuthServiceToken)) :
						null
				);
				await notificationService.CreatePasswordResetNotification(request);
			}
			return Ok();
		}
		[HttpPost]
		public async Task<IActionResult> PasswordCreationEmailDispatch(
			[FromServices] NotificationService notificationService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var latestRequest = db.GetLatestPasswordResetRequest(User.GetUserAccountId());
				if (IsPasswordResetRequestValid(latestRequest)) {
					return BadRequest(new[] { "RequestLimitExceeded" });
				}
				var request = await db.CreatePasswordResetRequest(
					userAccountId: User.GetUserAccountId(),
					authServiceAuthenticationId: null
				);
				await notificationService.CreatePasswordResetNotification(request);
			}
			return Ok();
		}
		[HttpGet]
		public async Task<IActionResult> GetUserAccount() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await db.GetUserAccountById(this.User.GetUserAccountId())
				);
			}
		}
		// deprecated
		[HttpGet]
		public async Task<IActionResult> GetSessionState() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(new {
					UserAccount = await db.GetUserAccountById(this.User.GetUserAccountId()),
					NewReplyNotification = new {
						LastReply = 0,
						LastNewReplyAck = 0,
						LastNewReplyDesktopNotification = 0,
						Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
					}
				});
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignIn(
			[FromServices] AuthenticationService authenticationService,
			[FromBody] SignInForm form
		) {
			// rate limit
			await Task.Delay(1000);
			UserAccount userAccount;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				userAccount = db.GetUserAccountByEmail(form.Email);
				if (userAccount == null) {
					return BadRequest(new[] { "UserAccountNotFound" });
				}
				if (!IsCorrectPassword(userAccount, form.Password)) {
					return BadRequest(new[] { "IncorrectPassword" });
				}
				if (form.AuthServiceToken != null) {
					var authentication = await db.GetAuthServiceAuthenticationById(
						ParseAuthServiceToken(form.AuthServiceToken)
					);
					var validationError = ValidateAuthServiceAuthenticationForAssociation(authentication);
					if (validationError != null) {
						return BadRequest(new[] { validationError });
					}
					var authServiceAccount = await db.AssociateAuthServiceAccount(
						identityId: authentication.IdentityId,
						authenticationId: authentication.Id,
						userAccountId: userAccount.Id,
						associationMethod: AuthServiceAssociationMethod.Manual
					);
					if (authServiceAccount.Provider == AuthServiceProvider.Twitter) {
						userAccount.HasLinkedTwitterAccount = true;
					}
				}
				await authenticationService.SignIn(userAccount, form.PushDevice);
				return Json(
					new WebAppUserProfileClientModel(
						await db.GetDisplayPreference(userAccount.Id),
						await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccount.Id),
						userAccount
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignOut(
			[FromServices] AuthenticationService authenticationService,
			[FromBody] SignOutForm form
		) {
			await authenticationService.SignOut(form.InstallationId);
			return Ok();
		}
		// deprecated
		[HttpPost]
		public async Task<IActionResult> UpdateNotificationPreferences(
			[FromBody] UpdateNotificationPreferencesBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var preference = await db.GetNotificationPreference(
					userAccountId: User.GetUserAccountId()
				);
				preference.ReplyViaEmail = binder.ReceiveEmailNotifications;
				preference.ReplyViaExtension = binder.ReceiveDesktopNotifications;
				preference = await db.SetNotificationPreference(
					userAccountId: preference.UserAccountId,
					options: preference
				);
				var user = await db.GetUserAccountById(
					userAccountId: preference.UserAccountId
				);
				return Json(user);
			}
		}
		// deprecated
		[HttpPost]
		public async Task<IActionResult> UpdateContactPreferences(
			[FromBody] UpdateContactPreferencesBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var preference = await db.GetNotificationPreference(
					userAccountId: User.GetUserAccountId()
				);
				preference.CompanyUpdateViaEmail = binder.ReceiveWebsiteUpdates;
				preference.AotdDigestViaEmail = binder.ReceiveSuggestedReadings ? NotificationEventFrequency.Weekly : NotificationEventFrequency.Never;
				preference = await db.SetNotificationPreference(
					userAccountId: preference.UserAccountId,
					options: preference
				);
				var user = await db.GetUserAccountById(
					userAccountId: preference.UserAccountId
				);
				return Json(user);
			}
		}
		// deprecated
		[HttpGet]
		public IActionResult CheckNewReplyNotification() {
			return Json(
				data: new {
					LastReply = 0,
					LastNewReplyAck = 0,
					LastNewReplyDesktopNotification = 0,
					Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
				}
			);
		}
		// deprecated
		[HttpPost]
		public IActionResult AckNewReply() {
			return Ok();
		}
		// deprecated
		[HttpPost]
		public IActionResult CreateDesktopNotification() {
			return BadRequest();
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<JsonResult> EmailSubscriptions(
			string token
		) {
			UserAccount userAccount;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				try {
					userAccount = await db.GetUserAccountById(Int64.Parse(StringEncryption.Decrypt(token, tokenOpts.EncryptionKey)));
				} catch (Exception ex) {
					log.LogError(ex, "Failed to parse email subscription token. Token: {Token}", token);
					userAccount = null;
				}
				if (userAccount != null) {
					return Json(
						new {
							IsValid = true,
							EmailAddress = userAccount.Email,
							Preference = new NotificationPreference(
								await db.GetNotificationPreference(userAccount.Id)
							)
						}
					);
				}
			}
			return Json(
				new {
					IsValid = false
				}
			);
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> UpdateEmailSubscriptions(
			[FromBody] UpdateEmailSubscriptionsBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var preference = await db.GetNotificationPreference(
					userAccountId: Int64.Parse(StringEncryption.Decrypt(binder.Token, tokenOpts.EncryptionKey))
				);
				if (preference != null) {
					await db.SetNotificationPreference(
						userAccountId: preference.UserAccountId,
						options: binder.GetOptions()
					);
					return Ok();
				}
			}
			return BadRequest();
		}
		// deprecated
		[AllowAnonymous]
		[HttpGet]
		public IActionResult PasswordResetRequest(
			string token,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			PasswordResetRequest request;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(token, tokenOpts.EncryptionKey)));
			}
			if (request == null) {
				return Redirect(serviceOpts.Value.WebServer.CreateUrl("/password/reset/not-found"));
			}
			if (IsPasswordResetRequestValid(request)) {
				return Redirect(serviceOpts.Value.WebServer.CreateUrl($"/?reset-password&email={WebUtility.UrlEncode(request.EmailAddress)}&token={WebUtility.UrlEncode(token)}"));
			}
			return Redirect(serviceOpts.Value.WebServer.CreateUrl("/password/reset/expired"));
		}
		// deprecated
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> ViewReply(
			string token,
			[FromServices] RoutingService routingService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return ReadReplyAndRedirectToArticle(
					reply: await db.GetComment(Int64.Parse(StringEncryption.Decrypt(token, tokenOpts.EncryptionKey))),
					routingService: routingService
				);
			}
		}
		// deprecated
		[HttpGet]
		public async Task<IActionResult> ViewReplyFromDesktopNotification(
			long id,
			[FromServices] RoutingService routingService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return ReadReplyAndRedirectToArticle(
					reply: await db.GetComment(id),
					routingService: routingService
				);
			}
		}
		[HttpGet]
		public JsonResult TimeZones() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					db
						.GetTimeZones()
						.GroupBy(zone => new { zone.DisplayName, zone.BaseUtcOffset })
						.OrderBy(group => group.Key.BaseUtcOffset)
						.ThenBy(group => group.Key.DisplayName)
						.Select(group => new {
							Key = group.Key.DisplayName,
							Value = group
								.OrderBy(zone => zone.Territory)
								.Select(zone => new {
									zone.Id,
									zone.Territory,
									zone.Name
								})
								.ToArray()
						})
						.ToArray()
				);
			}
		}
		[HttpPost]
		public JsonResult ChangeTimeZone([FromBody] ChangeTimeZoneBinder binder) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				long timeZoneId;
				if (binder.Id.HasValue) {
					timeZoneId = binder.Id.Value;
				} else {
					timeZoneId = GetTimeZoneIdFromName(db.GetTimeZones(), binder.Name);
				}
				return Json(
					db.UpdateTimeZone(this.User.GetUserAccountId(), timeZoneId)
				);
			}
		}
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		[HttpGet]
		public JsonResult Stats() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var users = db.GetUserAccounts();
				return Json(new {
					TotalCount = users.Count(),
					ConfirmedCount = users.Count(user => user.IsEmailConfirmed)
				});
			}
		}
		[HttpGet]
		public async Task<ActionResult<SettingsResponse>> Settings() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var user = await db.GetUserAccountById(
					userAccountId: User.GetUserAccountId()
				);
				var subscriptionStatus = await db.GetCurrentSubscriptionStatusForUserAccountAsync(user.Id);
				return new SettingsResponse(
					displayPreference: await db.GetDisplayPreference(user.Id),
					userCount: await db.GetUserCount(),
					notificationPreference: new NotificationPreference(
						options: await db.GetNotificationPreference(
							userAccountId: user.Id
						)
					),
					timeZoneDisplayName: (
						user.TimeZoneId.HasValue ?
							(
								await db.GetTimeZoneById(
									id: user.TimeZoneId.Value
								)
							)
							.DisplayName :
							null
					),
					authServiceAccounts: (await db.GetAuthServiceAccountsForUserAccount(user.Id))
						.Where(
							account => account.Provider == AuthServiceProvider.Apple || account.AccessTokenValue != null
						)
						.OrderByDescending(
							account => account.DateUserAccountAssociated
						)
						.Select(
							account => new AuthServiceAccountAssociation(account)
						),
					subscriptionStatus: SubscriptionStatusClientModel.FromSubscriptionStatus(user, subscriptionStatus),
					subscriptionPaymentMethod: (
						subscriptionStatus?.Provider == SubscriptionProvider.Stripe ?
							new SubscriptionPaymentMethodClientModel(
								await db.GetDefaultPaymentMethodForSubscriptionAccountAsync(
									provider: SubscriptionProvider.Stripe,
									providerAccountId: subscriptionStatus.ProviderAccountId
								)
							) :
							null
					)
				);
			}
		}
		[HttpPost]
		public async Task<JsonResult> NotificationPreference(
			[FromBody] NotificationPreference form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					data: new NotificationPreference(
						options: await db.SetNotificationPreference(
							userAccountId: User.GetUserAccountId(),
							options: form.GetOptions()
						)
					)
				);
			}
		}
		[HttpGet("UserAccounts/DisplayPreference")]
		public async Task<JsonResult> GetDisplayPreference() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await db.GetDisplayPreference(
						User.GetUserAccountId()
					)
				);
			}
		}
		[HttpPost("UserAccounts/DisplayPreference")]
		public async Task<JsonResult> SetDisplayPreference(
			[FromBody] DisplayPreferenceForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await db.SetDisplayPreference(
						userAccountId: User.GetUserAccountId(),
						theme: form.Theme,
						textSize: form.TextSize,
						hideLinks: form.HideLinks
					)
				);
			}
		}
		[HttpGet]
		public async Task<ActionResult<WebAppUserProfileClientModel>> WebAppUserProfile() {
			var userId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return new WebAppUserProfileClientModel(
					displayPreference: await db.GetDisplayPreference(userId),
					subscriptionStatus: await db.GetCurrentSubscriptionStatusForUserAccountAsync(userId),
					userAccount: await db.GetUserAccountById(userId)
				);
			}
		}
		[HttpPost]
		public async Task<JsonResult> OrientationCompletion() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountId();
				var userAccount = await db.RegisterOrientationCompletion(userAccountId);
				if (userAccount == null) {
					log.LogError("Duplicate orientation completion registration. UserId: {UserId}", userAccountId);
					userAccount = await db.GetUserAccountById(userAccountId);
				}
				return Json(userAccount);
			}
		}

		// new versions
		[AllowAnonymous]
		[HttpPost]
		public IActionResult ConfirmEmail2(
			[FromBody] ConfirmEmailForm form,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var emailConfirmationId = Int64.Parse(StringEncryption.Decrypt(form.Token, tokenOpts.EncryptionKey));
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var confirmation = db.GetEmailConfirmation(emailConfirmationId);
				if (confirmation == null) {
					return BadRequest("NotFound");
				}
				if (confirmation.DateConfirmed.HasValue) {
					return BadRequest("AlreadyConfirmed");
				}
				if (confirmation.Id != db.GetLatestUnconfirmedEmailConfirmation(confirmation.UserAccountId).Id) {
					return BadRequest("Expired");
				}
				db.ConfirmEmailAddress(emailConfirmationId);
				return Ok();
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult PasswordResetRequest2(
			string token,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			PasswordResetRequest request;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(token, tokenOpts.EncryptionKey)));
			}
			if (request == null) {
				return BadRequest("NotFound");
			}
			if (IsPasswordResetRequestValid(request)) {
				return Json(request);
			}
			return BadRequest("Expired");
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> ViewReply2(
			[FromBody] ViewReplyForm form,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts,
			[FromServices] ObfuscationService obfuscationService
		) {
			Int64 commentId;
			if (form.Id.HasValue) {
				if (User.Identity.IsAuthenticated) {
					commentId = form.Id.Value;
				} else {
					return BadRequest();
				}
			} else {
				commentId = Int64.Parse(StringEncryption.Decrypt(form.Token, tokenOpts.EncryptionKey));
			}
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var comment = await db.GetComment(commentId);
				return Json(new CommentThread(
					comment: comment,
					badge: (
							await db.GetUserLeaderboardRankings(
								userAccountId: comment.UserAccountId
							)
						)
						.GetBadge(),
					obfuscationService: obfuscationService
				));
			}
		}
   }
}