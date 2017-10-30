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

namespace api.Controllers.UserAccounts {
	public class UserAccountsController : Controller {
		private AuthenticationOptions authOpts;
		public UserAccountsController(IOptions<AuthenticationOptions> authOpts) {
			this.authOpts = authOpts.Value;
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
		private async Task SignIn(UserAccount userAccount) => await this.HttpContext.Authentication.SignInAsync(
			authenticationScheme: authOpts.Scheme,
			principal: new ClaimsPrincipal(new[] {
				new ClaimsIdentity(
					claims: new[] {
						new Claim(ClaimTypes.NameIdentifier, userAccount.Id.ToString()),
						new Claim(ClaimTypes.Role, userAccount.Role.ToString())
					},
					authenticationType: "ApplicationCookie"
				)
			}),
			properties: new AuthenticationProperties() {
				IsPersistent = true
			}
		);
		private IActionResult ReadReplyAndRedirectToArticle(Comment reply, DbConnection db, IOptions<ServiceEndpointsOptions> serviceOpts) {
			var slugParts = reply.ArticleSlug.Split('_');
			db.ReadComment(reply.Id);
			return Redirect(serviceOpts.Value.WebServer.CreateUrl($"/articles/{slugParts[0]}/{slugParts[1]}/{reply.Id}"));
		}
		private bool IsPasswordValid(string password) =>
			!String.IsNullOrWhiteSpace(password) &&
			password.Length >= 8 &&
			password.Length <= 256;
		private bool IsCorrectPassword(UserAccount userAccount, string password) =>
			userAccount.PasswordHash.SequenceEqual(HashPassword(password, userAccount.PasswordSalt));
		private bool IsEmailConfirmationRateExceeded(EmailConfirmation latestUnconfirmedConfirmation) =>
			latestUnconfirmedConfirmation != null ?
				DateTime.UtcNow.Subtract(latestUnconfirmedConfirmation.DateCreated).TotalHours < 24 :
				false;
		private bool IsPasswordResetRequestValid(PasswordResetRequest request) =>
				request != null &&
				!request.DateCompleted.HasValue &&
				DateTime.UtcNow.Subtract(request.DateCreated).TotalHours < 24;
		[AllowAnonymous]
		[HttpPost]
      public async Task<IActionResult> CreateAccount(
			[FromBody] CreateAccountBinder binder,
			[FromServices] IOptions<DatabaseOptions> dbOpts,
			[FromServices] EmailService emailService
		) {
			if (!IsPasswordValid(binder.Password)) {
				return BadRequest();
			}
			try {
				var salt = GenerateSalt();
				using (var db = new DbConnection(dbOpts)) {
					var userAccount = db.CreateUserAccount(binder.Name, binder.Email, HashPassword(binder.Password, salt), salt);
					await emailService.SendWelcomeEmail(
						recipient: userAccount,
						emailConfirmationId: db.CreateEmailConfirmation(userAccount.Id).Id
					);
					await SignIn(userAccount);
					return Json(userAccount);
				}
			} catch (Exception ex) {
				return BadRequest((ex as ValidationException)?.Errors);
			}
      }
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ConfirmEmail(
			string token,
			[FromServices] DbConnection db,
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var emailConfirmationId = new Guid(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey));
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
		[HttpPost]
		public async Task<IActionResult> ResendConfirmationEmail([FromServices] DbConnection db, [FromServices] EmailService emailService) {
			if (IsEmailConfirmationRateExceeded(db.GetLatestUnconfirmedEmailConfirmation(this.User.GetUserAccountId()))) {
				return BadRequest(new[] { "ResendLimitExceeded" });
			}
			var userAccount = db.GetUserAccount(this.User.GetUserAccountId());
			await emailService.SendConfirmationEmail(
				recipient: userAccount,
				emailConfirmationId: db.CreateEmailConfirmation(userAccount.Id).Id
			);
			return Ok();
		}
		[HttpPost]
		public IActionResult ChangePassword([FromBody] ChangePasswordBinder binder, [FromServices] DbConnection db) {
			if (!IsPasswordValid(binder.NewPassword)) {
				return BadRequest();
			}
			var userAccount = db.GetUserAccount(this.User.GetUserAccountId());
			if (!IsCorrectPassword(userAccount, binder.CurrentPassword)) {
				return BadRequest(new[] { "IncorrectPassword" });
			}
			var salt = GenerateSalt();
			db.ChangePassword(userAccount.Id, HashPassword(binder.NewPassword, salt), salt);
			return Ok();
		}
		[AllowAnonymous]
		[HttpPost]
		public IActionResult ResetPassword(
			[FromBody] ResetPasswordBinder binder,
			[FromServices] DbConnection db,
			[FromServices] IOptions<EmailOptions> emailOpts
		) {
			var request = db.GetPasswordResetRequest(new Guid(StringEncryption.Decrypt(binder.Token, emailOpts.Value.EncryptionKey)));
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
				return Ok();
			}
			return BadRequest(new[] { "RequestExpired" });
		}
		[HttpPost]
		public async Task<IActionResult> ChangeEmailAddress(
			[FromBody] EmailAddressBinder binder,
			[FromServices] DbConnection db,
			[FromServices] EmailService emailService
		) {
			var userAccount = db.GetUserAccount(this.User.GetUserAccountId());
			if (userAccount.Email == binder.Email) {
				return BadRequest();
			}
			var isEmailAddressConfirmed = db.IsEmailAddressConfirmed(userAccount.Id, binder.Email);
			if (
				isEmailAddressConfirmed ||
				!IsEmailConfirmationRateExceeded(db.GetLatestUnconfirmedEmailConfirmation(userAccount.Id))
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
				var updatedUserAccount = db.GetUserAccount(userAccount.Id);
				if (!isEmailAddressConfirmed) {
					await emailService.SendConfirmationEmail(
						recipient: updatedUserAccount,
						emailConfirmationId: confirmation.Id
					);
				}
				return Json(updatedUserAccount);
			}
			return BadRequest(new[] { "ResendLimitExceeded" });
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> RequestPasswordReset(
			[FromBody] EmailAddressBinder binder,
			[FromServices] DbConnection db,
			[FromServices] EmailService emailService
		) {
			var userAccount = db.FindUserAccount(binder.Email);
			if (userAccount == null) {
				return BadRequest(new[] { "UserAccountNotFound" });
			}
			var latestRequest = db.GetLatestPasswordResetRequest(userAccount.Id);
			if (IsPasswordResetRequestValid(latestRequest)) {
				return BadRequest(new[] { "RequestLimitExceeded" });
			}
			await emailService.SendPasswordResetEmail(userAccount, db.CreatePasswordResetRequest(userAccount.Id).Id);
			return Ok();
		}
		[HttpGet]
		public IActionResult GetUserAccount([FromServices] DbConnection db) => Json(db.GetUserAccount(this.User.GetUserAccountId()));
		[HttpGet]
		public IActionResult GetSessionState([FromServices] DbConnection db) {
			var userAccount = db.GetUserAccount(this.User.GetUserAccountId());
			return Json(new {
				UserAccount = userAccount,
				NewReplyNotification = new NewReplyNotification(userAccount, db.GetLatestUnreadReply(userAccount.Id))
			});
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignIn([FromBody] SignInBinder binder, [FromServices] DbConnection db) {
			var userAccount = db.FindUserAccount(binder.Email);
			if (userAccount == null) {
				return BadRequest(new[] { "UserAccountNotFound" });
			}
			if (!IsCorrectPassword(userAccount, binder.Password)) {
				return BadRequest(new[] { "IncorrectPassword" });
			}
			await SignIn(userAccount);
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
			[FromBody] UpdateNotificationPreferencesBinder binder,
			[FromServices] DbConnection db
		) => Json(db.UpdateNotificationPreferences(
			this.User.GetUserAccountId(),
			binder.ReceiveEmailNotifications,
			binder.ReceiveDesktopNotifications
		));
		[HttpPost]
		public IActionResult UpdateContactPreferences(
			[FromBody] UpdateContactPreferencesBinder binder,
			[FromServices] DbConnection db
		) => Json(db.UpdateContactPreferences(
			this.User.GetUserAccountId(),
			binder.ReceiveWebsiteUpdates,
			binder.ReceiveSuggestedReadings
		));
		[HttpGet]
		public IActionResult CheckNewReplyNotification([FromServices] DbConnection db) => Json(new NewReplyNotification(
			userAccount: db.GetUserAccount(this.User.GetUserAccountId()),
			latestUnreadReply: db.GetLatestUnreadReply(this.User.GetUserAccountId())
		));
		[HttpPost]
		public IActionResult AckNewReply([FromServices] DbConnection db) {
			db.AckNewReply(this.User.GetUserAccountId());
			return Ok();
		}
		[HttpPost]
		public IActionResult CreateDesktopNotification([FromServices] DbConnection db) {
			var userAccount = db.GetUserAccount(this.User.GetUserAccountId());
			db.RecordNewReplyDesktopNotification(userAccount.Id);
			if (userAccount.ReceiveReplyDesktopNotifications) {
				var latestUnreadReply = db.GetLatestUnreadReply(userAccount.Id);
				if (
					latestUnreadReply.DateCreated > userAccount.LastNewReplyAck &&
					latestUnreadReply.DateCreated > userAccount.LastNewReplyDesktopNotification
				) {
					return Json(latestUnreadReply);
				}
			}
			return BadRequest();
		}
		[AllowAnonymous]
		[HttpGet]
		public JsonResult EmailSubscriptions(
			string token,
			[FromServices] DbConnection db,
			[FromServices] IOptions<EmailOptions> emailOpts
		) {
			var userAccount = db.GetUserAccount(new Guid(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey)));
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
		public IActionResult UpdateEmailSubscriptions(
			[FromBody] UpdateEmailSubscriptionsBinder binder,
			[FromServices] DbConnection db,
			[FromServices] IOptions<EmailOptions> emailOpts
		) {
			var userAccount = db.GetUserAccount(new Guid(StringEncryption.Decrypt(binder.Token, emailOpts.Value.EncryptionKey)));
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
			return BadRequest();
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult PasswordResetRequest(
			string token,
			[FromServices] DbConnection db,
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var request = db.GetPasswordResetRequest(new Guid(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey)));
			if (request == null) {
				return Redirect(serviceOpts.Value.WebServer.CreateUrl("/password/reset/not-found"));
			}
			if (IsPasswordResetRequestValid(request)) {
				return Redirect(serviceOpts.Value.WebServer.CreateUrl($"/?reset-password&email={WebUtility.UrlEncode(request.EmailAddress)}&token={WebUtility.UrlEncode(token)}"));
			}
			return Redirect(serviceOpts.Value.WebServer.CreateUrl("/password/reset/expired"));
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ViewReply(
			string token,
			[FromServices] DbConnection db,
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) => ReadReplyAndRedirectToArticle(
			reply: db.GetComment(new Guid(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey))),
			db: db,
			serviceOpts: serviceOpts
		);
		[HttpGet]
		public IActionResult ViewReplyFromDesktopNotification(
			Guid id,
			[FromServices] DbConnection db,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) => ReadReplyAndRedirectToArticle(
			reply: db.GetComment(id),
			db: db,
			serviceOpts: serviceOpts
		);
   }
}