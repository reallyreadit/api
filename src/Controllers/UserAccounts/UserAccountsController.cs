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
		private static byte[] HashPassword(string password, byte[] salt) {
			return KeyDerivation.Pbkdf2(
				password: password,
				salt: salt,
				prf: KeyDerivationPrf.HMACSHA1,
				iterationCount: 10000,
				numBytesRequested: 256 / 8
			);
		}
		private async Task SignIn(Guid userId) => await this.HttpContext.Authentication.SignInAsync(
			authenticationScheme: authOpts.Scheme,
			principal: new ClaimsPrincipal(new[] {
				new ClaimsIdentity(
					claims: new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
					authenticationType: "ApplicationCookie"
				)
			}),
			properties: new AuthenticationProperties() {
				IsPersistent = true
			}
		);
		private Int64 CreateUnixTimestamp(DateTime date) => new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeMilliseconds();
		[AllowAnonymous]
		[HttpPost]
      public async Task<IActionResult> CreateAccount(
			[FromBody] CreateAccountBinder binder,
			[FromServices] IOptions<DatabaseOptions> dbOpts,
			[FromServices] EmailService emailService
		) {
			if (
				String.IsNullOrWhiteSpace(binder.Password) ||
				binder.Password.Length < 8 ||
				binder.Password.Length > 256
			) {
				return BadRequest();
			}
			try {
				var salt = GenerateSalt();
				using (var db = new DbConnection(dbOpts)) {
					var userAccount = db.CreateUserAccount(binder.Name, binder.Email, HashPassword(binder.Password, salt), salt);
					await emailService.SendConfirmationEmail(
						recipient: userAccount,
						emailConfirmationId: db.CreateEmailConfirmation(userAccount.Id).Id
					);
					await SignIn(userAccount.Id);
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
				return Redirect(resultBaseUrl + "/notFound");
			}
			var latestConfirmation = db.GetLatestEmailConfirmation(confirmation.UserAccountId);
			if (confirmation.Id != latestConfirmation.Id) {
				return Redirect(resultBaseUrl + "/expired");
			}
			if (confirmation.DateConfirmed.HasValue) {
				return Redirect(resultBaseUrl + "/alreadyConfirmed");
			}
			db.ConfirmEmailAddress(emailConfirmationId);
			return Redirect(resultBaseUrl + "/success");
		}
		[HttpPost]
		public async Task<IActionResult> ResendConfirmationEmail([FromServices] DbConnection db, [FromServices] EmailService emailService) {
			var latestConfirmation = db.GetLatestEmailConfirmation(this.User.GetUserAccountId());
			if (DateTime.UtcNow.Subtract(latestConfirmation.DateCreated).TotalHours < 24) {
				return BadRequest(new[] { "ResendLimitExceeded" });
			}
			if (!latestConfirmation.DateConfirmed.HasValue) {
				var userAccount = db.GetUserAccount(this.User.GetUserAccountId());
				await emailService.SendConfirmationEmail(
					recipient: userAccount,
					emailConfirmationId: db.CreateEmailConfirmation(userAccount.Id).Id
				);
			}
			return Ok();
		}
		[HttpGet]
		public IActionResult GetUserAccount([FromServices] DbConnection db) => Json(db.GetUserAccount(this.User.GetUserAccountId()));
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignIn([FromBody] SignInBinder binder, [FromServices] DbConnection db) {
			var userAccount = db.FindUserAccount(binder.Email);
			if (userAccount == null) {
				return BadRequest(new[] { "UserAccountNotFound" });
			}
			if (!userAccount.PasswordHash.SequenceEqual(HashPassword(binder.Password, userAccount.PasswordSalt))) {
				return BadRequest(new[] { "IncorrectPassword" });
			}
			await SignIn(userAccount.Id);
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
		[HttpGet]
		public IActionResult CheckNewReplyNotification([FromServices] DbConnection db) {
			var userAccount = db.GetUserAccount(this.User.GetUserAccountId());
			var lastReplyDate = db.GetLatestReplyDate(userAccount.Id);
			return Json(new {
				LastReply = lastReplyDate.HasValue ? CreateUnixTimestamp(lastReplyDate.Value) : 0,
				LastNewReplyAck = CreateUnixTimestamp(userAccount.LastNewReplyAck),
				LastNewReplyDesktopNotification = CreateUnixTimestamp(userAccount.LastNewReplyDesktopNotification)
			});
		}
		[HttpPost]
		public IActionResult AckNewReply([FromServices] DbConnection db) {
			db.AckNewReply(this.User.GetUserAccountId());
			return Ok();
		}
		[HttpPost]
		public IActionResult RecordNewReplyDesktopNotification([FromServices] DbConnection db) {
			db.RecordNewReplyDesktopNotification(this.User.GetUserAccountId());
			return Ok();
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult Unsubscribe(
			string token,
			[FromServices] DbConnection db,
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var userAccount = db.GetUserAccount(new Guid(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey)));
			var resultBaseUrl = serviceOpts.Value.WebServer.CreateUrl("/email/unsubscribe");
			if (userAccount.ReceiveReplyEmailNotifications) {
					db.UpdateNotificationPreferences(
					userAccountId: userAccount.Id,
					receiveReplyEmailNotifications: false,
					receiveReplyDesktopNotifications: userAccount.ReceiveReplyDesktopNotifications
				);
				return Redirect(resultBaseUrl + "/success");
			}
			return Redirect(resultBaseUrl + "/alreadyUnsubscribed");
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ViewReply(
			string token,
			[FromServices] DbConnection db,
			[FromServices] IOptions<EmailOptions> emailOpts,
			[FromServices] IOptions<ServiceEndpointsOptions> serviceOpts
		) {
			var comment = db.GetComment(new Guid(StringEncryption.Decrypt(token, emailOpts.Value.EncryptionKey)));
			var slugParts = comment.ArticleSlug.Split('_');
			db.ReadComment(comment.Id);
			return Redirect(serviceOpts.Value.WebServer.CreateUrl($"/articles/{slugParts[0]}/{slugParts[1]}/{comment.Id}"));
		}
   }
}