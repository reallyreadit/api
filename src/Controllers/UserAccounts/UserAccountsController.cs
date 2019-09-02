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
using Microsoft.AspNetCore.Http.Authentication;
using api.DataAccess.Models;
using System.Net;
using api.Authorization;
using Npgsql;
using System.IO;
using api.Security;
using api.ClientModels;
using api.Analytics;
using api.DataAccess.Stats;

namespace api.Controllers.UserAccounts {
	public class UserAccountsController : Controller {
		private AuthenticationOptions authOpts;
		private DatabaseOptions dbOpts;
		public UserAccountsController(IOptions<AuthenticationOptions> authOpts, IOptions<DatabaseOptions> dbOpts) {
			this.authOpts = authOpts.Value;
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
		private static long GetTimeZoneIdFromName(IEnumerable<TimeZone> timeZones, string timeZoneName) => timeZones
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
		[AllowAnonymous]
		[HttpPost]
      public async Task<IActionResult> CreateAccount(
			[FromBody] CreateAccountBinder binder,
			[FromServices] EmailService emailService,
			[FromServices] CaptchaService captchaService
		) {
			if (!IsPasswordValid(binder.Password)) {
				return BadRequest();
			}
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var captchaResponse = await captchaService.Verify(binder.CaptchaResponse);
				if (captchaResponse != null) {
					db.CreateCaptchaResponse("createUserAccount", captchaResponse);
				}
				try {
					var salt = GenerateSalt();
					var userAccount = db.CreateUserAccount(
						name: binder.Name,
						email: binder.Email,
						passwordHash: HashPassword(binder.Password, salt),
						passwordSalt: salt,
						timeZoneId: GetTimeZoneIdFromName(db.GetTimeZones(), binder.TimeZoneName),
						analytics: new UserAccountCreationAnalytics() {
							Client = this.GetRequestAnalytics().Client,
							MarketingScreenVariant = binder.MarketingScreenVariant,
							ReferrerUrl = binder.ReferrerUrl,
							InitialPath = binder.InitialPath
						}
					);
					await emailService.SendWelcomeEmail(
						recipient: userAccount,
						emailConfirmationId: db.CreateEmailConfirmation(userAccount.Id).Id
					);
					await HttpContext.Authentication.SignInAsync(authOpts.Scheme, userAccount);
					return Json(userAccount);
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
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var emailConfirmationId = Int64.Parse(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey));
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
				await emailService.SendConfirmationEmail(
					recipient: userAccount,
					emailConfirmationId: db.CreateEmailConfirmation(userAccount.Id).Id
				);
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
			[FromBody] ResetPasswordBinder binder,
			[FromServices] IOptions<EmailOptions> emailOpts
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(binder.Token, emailOpts.Value.EncryptionKey)));
				if (request == null) {
					return BadRequest(new[] { "RequestNotFound" });
				}
				if (IsPasswordResetRequestValid(request)) {
					if (!IsPasswordValid(binder.Password)) {
						return BadRequest();
					}
					var salt = GenerateSalt();
					db.ChangePassword(request.UserAccountId, HashPassword(binder.Password, salt), salt);
					db.CompletePasswordResetRequest(request.Id);
					var userAccount = await db.GetUserAccountById(request.UserAccountId);
					await HttpContext.Authentication.SignInAsync(authOpts.Scheme, userAccount);
					return Json(userAccount);
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
						await emailService.SendConfirmationEmail(
							recipient: updatedUserAccount,
							emailConfirmationId: confirmation.Id
						);
					}
					return Json(updatedUserAccount);
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
				await emailService.SendPasswordResetEmail(userAccount, db.CreatePasswordResetRequest(userAccount.Id).Id);
			}
			return Ok();
		}
		[HttpGet]
		public async Task<IActionResult> GetUserAccount() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetUserAccountById(this.User.GetUserAccountId()));
			}
		}
		[HttpGet]
		public async Task<IActionResult> GetSessionState() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccount = await db.GetUserAccountById(this.User.GetUserAccountId());
				return Json(new {
					UserAccount = userAccount,
					NewReplyNotification = new NewReplyNotification(userAccount, db.GetLatestUnreadReply(userAccount.Id))
				});
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignIn([FromBody] SignInBinder binder) {
			// rate limit
			await Task.Delay(1000);
			UserAccount userAccount;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				userAccount = db.GetUserAccountByEmail(binder.Email);
			}
			if (userAccount == null) {
				return BadRequest(new[] { "UserAccountNotFound" });
			}
			if (!IsCorrectPassword(userAccount, binder.Password)) {
				return BadRequest(new[] { "IncorrectPassword" });
			}
			await HttpContext.Authentication.SignInAsync(authOpts.Scheme, userAccount);
			return Json(userAccount);
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignOut() {
			await this.HttpContext.Authentication.SignOutAsync(authOpts.Scheme);
			return Ok();
		}
		[HttpPost]
		public IActionResult UpdateNotificationPreferences(
			[FromBody] UpdateNotificationPreferencesBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.UpdateNotificationPreferences(
					this.User.GetUserAccountId(),
					binder.ReceiveEmailNotifications,
					binder.ReceiveDesktopNotifications
				));
			}
		}
		[HttpPost]
		public IActionResult UpdateContactPreferences(
			[FromBody] UpdateContactPreferencesBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.UpdateContactPreferences(
					this.User.GetUserAccountId(),
					binder.ReceiveWebsiteUpdates,
					binder.ReceiveSuggestedReadings
				));
			}
		}
		[HttpGet]
		public async Task<IActionResult> CheckNewReplyNotification() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(new NewReplyNotification(
					userAccount: await db.GetUserAccountById(this.User.GetUserAccountId()),
					latestUnreadReply: db.GetLatestUnreadReply(this.User.GetUserAccountId())
				));
			}
		}
		[HttpPost]
		public IActionResult AckNewReply() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.AckNewReply(this.User.GetUserAccountId());
			}
			return Ok();
		}
		[HttpPost]
		public async Task<IActionResult> CreateDesktopNotification(
			[FromServices] IOptions<EmailOptions> emailOptions
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccount = await db.GetUserAccountById(this.User.GetUserAccountId());
				db.RecordNewReplyDesktopNotification(userAccount.Id);
				if (userAccount.ReceiveReplyDesktopNotifications) {
					var latestUnreadReply = db.GetLatestUnreadReply(userAccount.Id);
					if (
						latestUnreadReply.DateCreated > userAccount.LastNewReplyAck &&
						latestUnreadReply.DateCreated > userAccount.LastNewReplyDesktopNotification
					) {
						return Json(
							new {
								ArticleTitle = latestUnreadReply.ArticleTitle,
								Token = StringEncryption.Encrypt(latestUnreadReply.Id.ToString(), emailOptions.Value.EncryptionKey),
								UserName = latestUnreadReply.UserAccount
							}
						);
					}
				}
			}
			return BadRequest();
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<JsonResult> EmailSubscriptions(
			string token,
			[FromServices] IOptions<EmailOptions> emailOpts
		) {
			UserAccount userAccount;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				userAccount = await db.GetUserAccountById(Int64.Parse(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey)));
			}
			if (userAccount != null) {
				return Json(new {
					IsValid = true,
					EmailAddress = userAccount.Email,
					Subscriptions = new {
						CommentReplyNotifications = userAccount.ReceiveReplyEmailNotifications,
						WebsiteUpdates = userAccount.ReceiveWebsiteUpdates,
						SuggestedReadings = userAccount.ReceiveSuggestedReadings
					}
				});
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
			[FromServices] IOptions<EmailOptions> emailOpts
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccount = await db.GetUserAccountById(Int64.Parse(StringEncryption.Decrypt(binder.Token, emailOpts.Value.EncryptionKey)));
				if (userAccount != null) {
					db.UpdateNotificationPreferences(
						userAccountId: userAccount.Id,
						receiveReplyEmailNotifications: binder.CommentReplyNotifications,
						receiveReplyDesktopNotifications: userAccount.ReceiveReplyDesktopNotifications
					);
					db.UpdateContactPreferences(
						userAccountId: userAccount.Id,
						receiveWebsiteUpdates: binder.WebsiteUpdates,
						receiveSuggestedReadings: binder.SuggestedReadings
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
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			PasswordResetRequest request;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey)));
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
		public IActionResult ViewReply(
			string token,
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return ReadReplyAndRedirectToArticle(
					reply: db.GetComment(Int64.Parse(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey))),
					serviceOpts: serviceOpts
				);
			}
		}
		// deprecated
		[HttpGet]
		public IActionResult ViewReplyFromDesktopNotification(
			long id,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return ReadReplyAndRedirectToArticle(
					reply: db.GetComment(id),
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
						.Select(group => new KeyValuePair<string, object[]>(
							key: group.Key.DisplayName,
							value: group
								.OrderBy(zone => zone.Territory)
								.Select(zone => new {
									zone.Id,
									zone.Territory,
									zone.Name
								})
								.ToArray()
						))
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
				return Json(db.UpdateTimeZone(this.User.GetUserAccountId(), timeZoneId));
			}
		}
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		[HttpGet]
		public JsonResult Stats() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var users = db.ListUserAccounts();
				return Json(new {
					TotalCount = users.Count(),
					ConfirmedCount = users.Count(user => user.IsEmailConfirmed)
				});
			}
		}

		// new versions
		[AllowAnonymous]
		[HttpPost]
		public IActionResult ConfirmEmail2(
			[FromBody] ConfirmEmailForm form,
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var emailConfirmationId = Int64.Parse(StringEncryption.Decrypt(form.Token, emailOpts.Value.EncryptionKey));
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
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			PasswordResetRequest request;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				request = db.GetPasswordResetRequest(Int64.Parse(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey)));
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
			[FromServices] IOptions<EmailOptions> emailOpts,
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
				commentId = Int64.Parse(StringEncryption.Decrypt(form.Token, emailOpts.Value.EncryptionKey));
			}
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.ReadComment(commentId);
				var comment = db.GetComment(commentId);
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