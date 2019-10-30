using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using api.DataAccess;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using api.Configuration;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Collections.Generic;
using Amazon;
using Mvc.RenderViewToString;
using api.Messaging;
using api.Encryption;
using Microsoft.AspNetCore.Authentication;
using api.DataAccess.Models;
using System.Net;
using api.Authorization;
using Npgsql;
using System.IO;
using api.Security;
using api.ClientModels;
using api.Analytics;
using api.DataAccess.Stats;
using api.BackwardsCompatibility;

namespace api.Controllers.UserAccounts {
	public class UserAccountsController : Controller {
		private DatabaseOptions dbOpts;
		public UserAccountsController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
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
		private static long GetTimeZoneIdFromName(IEnumerable<api.DataAccess.Models.TimeZone> timeZones, string timeZoneName) => timeZones
			.Where(zone => zone.Name == timeZoneName)
			.OrderBy(zone => zone.Territory)
			.First()
			.Id;
		// deprecated
		private IActionResult ReadReplyAndRedirectToArticle(Comment reply, IOptions<ServiceEndpointsOptions> serviceOpts) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.ReadComment(reply.Id);
			}
			return Redirect(serviceOpts.Value.WebServer.CreateUrl(RouteHelper.GetArticlePath(reply.ArticleSlug) + "/" + reply.Id.ToString()));
		}
		private bool IsPasswordValid(string password) =>
			!String.IsNullOrWhiteSpace(password) &&
			password.Length >= 8 &&
			password.Length <= 256;
		private bool IsCorrectPassword(UserAccount userAccount, string password) =>
			userAccount.PasswordHash.SequenceEqual(HashPassword(password, userAccount.PasswordSalt));
		private bool IsEmailConfirmationRateExceeded(EmailConfirmation latestUnconfirmedConfirmation) =>
			latestUnconfirmedConfirmation != null ?
				DateTime.UtcNow.Subtract(latestUnconfirmedConfirmation.DateCreated).TotalMinutes < 5 :
				false;
		private bool IsPasswordResetRequestValid(PasswordResetRequest request) =>
				request != null &&
				!request.DateCompleted.HasValue &&
				DateTime.UtcNow.Subtract(request.DateCreated).TotalHours < 24;
		private async Task<Object> GetUserForClient(
			UserAccount user,
			NpgsqlConnection db
		) => (
			this.ClientVersionIsGreaterThanOrEqualTo(
				versions: new Dictionary<ClientType, SemanticVersion>() {
					{ ClientType.IosApp, new SemanticVersion(5, 0, 0) },
					{ ClientType.WebAppClient, new SemanticVersion(1, 8, 0) },
					{ ClientType.WebAppServer, new SemanticVersion(1, 8, 0) }
				}
			) ?
				user :
				new UserAccount_1_2_0(
					user: user,
					preference: await db.GetNotificationPreference(
						userAccountId: user.Id
					),
					timeZone: (
						user.TimeZoneId.HasValue ?
							await db.GetTimeZoneById(
								id: user.TimeZoneId.Value
							) :
							null
					)
				) as Object
		);
		private async Task<JsonResult> JsonUser(
			UserAccount user,
			NpgsqlConnection db
		) => (
			Json(
				data: await GetUserForClient(
					user: user,
					db: db
				)
			)
		);
		private async Task SignInUser(
			UserAccount user,
			PushDeviceForm pushDeviceForm,
			NpgsqlConnection db
		) {
			await HttpContext.SignInAsync(user);
			if (pushDeviceForm?.IsValid() ?? false) {
				await db.RegisterNotificationPushDevice(
					userAccountId: user.Id,
					installationId: pushDeviceForm.InstallationId,
					name: pushDeviceForm.Name,
					token: pushDeviceForm.Token
				);
			}
		}
		[AllowAnonymous]
		[HttpPost]
      public async Task<IActionResult> CreateAccount(
			[FromBody] UserAccountForm form,
			[FromServices] EmailService emailService,
			[FromServices] CaptchaService captchaService
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
					var userAccount = db.CreateUserAccount(
						name: form.Name,
						email: form.Email,
						passwordHash: HashPassword(form.Password, salt),
						passwordSalt: salt,
						timeZoneId: GetTimeZoneIdFromName(db.GetTimeZones(), form.TimeZoneName),
						analytics: new UserAccountCreationAnalytics() {
							Client = this.GetRequestAnalytics().Client,
							MarketingScreenVariant = form.MarketingScreenVariant,
							ReferrerUrl = form.ReferrerUrl,
							InitialPath = form.InitialPath
						}
					);
					/*await emailService.SendWelcomeEmail(
						recipient: userAccount,
						emailConfirmationId: db.CreateEmailConfirmation(userAccount.Id).Id
					);*/
					await SignInUser(
						user: userAccount,
						pushDeviceForm: form.PushDevice,
						db: db
					);
					return await JsonUser(
						user: userAccount,
						db: db
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
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var emailConfirmationId = Int64.Parse(StringEncryption.Decrypt(token, tokenizationOptions.Value.EncryptionKey));
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
		public async Task<IActionResult> ResendConfirmationEmail([FromServices] EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				if (IsEmailConfirmationRateExceeded(db.GetLatestUnconfirmedEmailConfirmation(this.User.GetUserAccountId()))) {
					return BadRequest(new[] { "ResendLimitExceeded" });
				}
				var userAccount = await db.GetUserAccountById(this.User.GetUserAccountId());
				// await emailService.SendConfirmationEmail(
				// 	recipient: userAccount,
				// 	emailConfirmationId: db.CreateEmailConfirmation(userAccount.Id).Id
				// );
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
			[FromBody] PasswordResetForm form,
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(form.Token, tokenizationOptions.Value.EncryptionKey)));
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
					var userAccount = await db.GetUserAccountById(request.UserAccountId);
					await SignInUser(
						user: userAccount,
						pushDeviceForm: form.PushDevice,
						db: db
					);
					return await JsonUser(
						user: userAccount,
						db: db
					);
				}
			}
			return BadRequest(new[] { "RequestExpired" });
		}
		[HttpPost]
		public async Task<IActionResult> ChangeEmailAddress(
			[FromBody] EmailAddressBinder binder,
			[FromServices] EmailService emailService
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
						// await emailService.SendConfirmationEmail(
						// 	recipient: updatedUserAccount,
						// 	emailConfirmationId: confirmation.Id
						// );
					}
					return await JsonUser(
						user: updatedUserAccount,
						db: db
					);
				}
			}
			return BadRequest(new[] { "ResendLimitExceeded" });
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> RequestPasswordReset(
			[FromBody] PasswordResetRequestBinder binder,
			[FromServices] EmailService emailService,
			[FromServices] CaptchaService captchaService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var captchaResponse = await captchaService.Verify(binder.CaptchaResponse);
				if (captchaResponse != null) {
					db.CreateCaptchaResponse("requestPasswordReset", captchaResponse);
				}
				var userAccount = db.GetUserAccountByEmail(binder.Email);
				if (userAccount == null) {
					return BadRequest(new[] { "UserAccountNotFound" });
				}
				var latestRequest = db.GetLatestPasswordResetRequest(userAccount.Id);
				if (IsPasswordResetRequestValid(latestRequest)) {
					return BadRequest(new[] { "RequestLimitExceeded" });
				}
				// await emailService.SendPasswordResetEmail(userAccount, db.CreatePasswordResetRequest(userAccount.Id).Id);
			}
			return Ok();
		}
		[HttpGet]
		public async Task<IActionResult> GetUserAccount() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return await JsonUser(
					user: await db.GetUserAccountById(this.User.GetUserAccountId()),
					db: db
				);
			}
		}
		// deprecated
		[HttpGet]
		public async Task<IActionResult> GetSessionState() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(new {
					UserAccount = await GetUserForClient(
						user: await db.GetUserAccountById(this.User.GetUserAccountId()),
						db: db
					),
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
				await SignInUser(
					user: userAccount,
					pushDeviceForm: form.PushDevice,
					db: db
				);
				return await JsonUser(
					user: userAccount,
					db: db
				);
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignOut(
			[FromBody] SignOutForm form
		) {
			if (
				User.Identity.IsAuthenticated &&
				!String.IsNullOrWhiteSpace(form?.InstallationId)
			) {
				using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
					await db.UnregisterNotificationPushDeviceByInstallationId(
						installationId: form.InstallationId,
						reason: NotificationPushUnregistrationReason.SignOut
					);
				}
			}
			await this.HttpContext.SignOutAsync();
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
				return await JsonUser(
					user: user,
					db: db
				);
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
				preference.SuggestedReadingViaEmail = binder.ReceiveSuggestedReadings;
				preference = await db.SetNotificationPreference(
					userAccountId: preference.UserAccountId,
					options: preference
				);
				var user = await db.GetUserAccountById(
					userAccountId: preference.UserAccountId
				);
				return await JsonUser(
					user: user,
					db: db
				);
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
			string token,
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions
		) {
			UserAccount userAccount;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				userAccount = await db.GetUserAccountById(Int64.Parse(StringEncryption.Decrypt(token, tokenizationOptions.Value.EncryptionKey)));
				if (userAccount != null) {
					var preference = await db.GetNotificationPreference(
						userAccountId: userAccount.Id
					);
					return Json(new {
						IsValid = true,
						EmailAddress = userAccount.Email,
						Subscriptions = new {
							CommentReplyNotifications = preference.ReplyViaEmail,
							WebsiteUpdates = preference.CompanyUpdateViaEmail,
							SuggestedReadings = preference.SuggestedReadingViaEmail
						}
					});
				}
			}
			return Json(new {
				IsValid = false,
				EmailAddress = null as string,
				Subscriptions = new {
					CommentReplyNotifications = false,
					WebsiteUpdates = false,
					SuggestedReadings = false
				}
			});
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> UpdateEmailSubscriptions(
			[FromBody] UpdateEmailSubscriptionsBinder binder,
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var preference = await db.GetNotificationPreference(
					userAccountId: Int64.Parse(StringEncryption.Decrypt(binder.Token, tokenizationOptions.Value.EncryptionKey))
				);
				if (preference != null) {
					preference.CompanyUpdateViaEmail = binder.WebsiteUpdates;
					preference.SuggestedReadingViaEmail = binder.SuggestedReadings;
					preference.ReplyViaEmail = binder.CommentReplyNotifications;
					await db.SetNotificationPreference(
						userAccountId: preference.UserAccountId,
						options: preference
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
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			PasswordResetRequest request;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(token, tokenizationOptions.Value.EncryptionKey)));
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
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return ReadReplyAndRedirectToArticle(
					reply: await db.GetComment(Int64.Parse(StringEncryption.Decrypt(token, tokenizationOptions.Value.EncryptionKey))),
					serviceOpts: serviceOpts
				);
			}
		}
		// deprecated
		[HttpGet]
		public async Task<IActionResult> ViewReplyFromDesktopNotification(
			long id,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return ReadReplyAndRedirectToArticle(
					reply: await db.GetComment(id),
					serviceOpts: serviceOpts
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
		public async Task<JsonResult> ChangeTimeZone([FromBody] ChangeTimeZoneBinder binder) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				long timeZoneId;
				if (binder.Id.HasValue) {
					timeZoneId = binder.Id.Value;
				} else {
					timeZoneId = GetTimeZoneIdFromName(db.GetTimeZones(), binder.Name);
				}
				return await JsonUser(
					user: db.UpdateTimeZone(this.User.GetUserAccountId(), timeZoneId),
					db: db
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
		public async Task<JsonResult> Settings() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var user = await db.GetUserAccountById(
					userAccountId: User.GetUserAccountId()
				);
				return Json(
					data: new {
						UserCount = await db.GetUserCount(),
						NotificationPreference = new NotificationPreference(
							options: await db.GetNotificationPreference(
								userAccountId: user.Id
							)
						),
						TimeZoneDisplayName = (
							user.TimeZoneId.HasValue ?
								(
									await db.GetTimeZoneById(
										id: user.TimeZoneId.Value
									)
								)
								.DisplayName :
								null
						)
					}
				);
			}
		}
		[HttpPost]
		public async Task<JsonResult> NotificationPreference(
			[FromBody] NotificationPreference form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var options = new NotificationPreferenceOptions() {
					CompanyUpdateViaEmail = form.CompanyUpdate == NotificationChannel.Email,
					SuggestedReadingViaEmail = form.SuggestedReading == NotificationEventFrequency.Weekly,
					AotdViaEmail = form.Aotd.HasFlag(NotificationChannel.Email),
					AotdViaExtension = form.Aotd.HasFlag(NotificationChannel.Extension),
					AotdViaPush = form.Aotd.HasFlag(NotificationChannel.Push),
					ReplyDigestViaEmail = form.ReplyDigest,
					LoopbackDigestViaEmail = form.LoopbackDigest,
					PostDigestViaEmail = form.PostDigest,
					FollowerDigestViaEmail = form.FollowerDigest
				};
				if (options.ReplyDigestViaEmail == NotificationEventFrequency.Never) {
					options.ReplyViaEmail = form.Reply.HasFlag(NotificationChannel.Email);
					options.ReplyViaExtension = form.Reply.HasFlag(NotificationChannel.Extension);
					options.ReplyViaPush = form.Reply.HasFlag(NotificationChannel.Push);
				}
				if (options.LoopbackDigestViaEmail == NotificationEventFrequency.Never) {
					options.LoopbackViaEmail = form.Loopback.HasFlag(NotificationChannel.Email);
					options.LoopbackViaExtension = form.Loopback.HasFlag(NotificationChannel.Extension);
					options.LoopbackViaPush = form.Loopback.HasFlag(NotificationChannel.Push);
				}
				if (options.PostDigestViaEmail == NotificationEventFrequency.Never) {
					options.PostViaEmail = form.Post.HasFlag(NotificationChannel.Email);
					options.PostViaExtension = form.Post.HasFlag(NotificationChannel.Extension);
					options.PostViaPush = form.Post.HasFlag(NotificationChannel.Push);
				}
				if (options.FollowerDigestViaEmail == NotificationEventFrequency.Never) {
					options.FollowerViaEmail = form.Follower.HasFlag(NotificationChannel.Email);
					options.FollowerViaExtension = form.Follower.HasFlag(NotificationChannel.Extension);
					options.FollowerViaPush = form.Follower.HasFlag(NotificationChannel.Push);
				}
				return Json(
					data: new NotificationPreference(
						options: await db.SetNotificationPreference(
							userAccountId: User.GetUserAccountId(),
							options: options
						)
					)
				);
			}
		}

		// new versions
		[AllowAnonymous]
		[HttpPost]
		public IActionResult ConfirmEmail2(
			[FromBody] ConfirmEmailForm form,
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var emailConfirmationId = Int64.Parse(StringEncryption.Decrypt(form.Token, tokenizationOptions.Value.EncryptionKey));
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
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			PasswordResetRequest request;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(token, tokenizationOptions.Value.EncryptionKey)));
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
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
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
				commentId = Int64.Parse(StringEncryption.Decrypt(form.Token, tokenizationOptions.Value.EncryptionKey));
			}
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.ReadComment(commentId);
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