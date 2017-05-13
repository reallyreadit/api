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
		private async Task SignIn(Guid userId) {
			await this.HttpContext.Authentication.SignInAsync(
				authenticationScheme: authOpts.Scheme,
				principal: new ClaimsPrincipal(new[] {
					new ClaimsIdentity(
						claims: new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
						authenticationType: "ApplicationCookie"
					)
				})
			);
		}
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
						recipient: new EmailMailbox(userAccount.Name, userAccount.Email),
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
		public IActionResult ConfirmEmail(Guid id, [FromServices] DbConnection db, [FromServices] IOptions<ServiceEndpointsOptions> serviceOpts) {
			var confirmation = db.GetEmailConfirmation(id);
			var resultBaseUrl = serviceOpts.Value.WebServer.CreateUrl("/emailConfirmation");
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
			db.ConfirmEmailAddress(id);
			return Redirect(resultBaseUrl + "/success");
		}
		[HttpGet]
		public IActionResult GetUserAccount([FromServices] DbConnection db) {
			return Json(db.GetUserAccount(this.User.GetUserAccountId()));
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignIn([FromBody] SignInBinder binder, [FromServices] DbConnection db) {
			var userAccount = db.FindUserAccount(binder.Name);
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
   }
}