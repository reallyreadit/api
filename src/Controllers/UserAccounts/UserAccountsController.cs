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

namespace api.Controllers.UserAccounts {
	public class UserAccountsController : Controller {
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
				authenticationScheme: Startup.AuthenticationScheme,
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
      public async Task<IActionResult> CreateAccount([FromBody] CreateAccountBinder binder) {
			if (
				String.IsNullOrWhiteSpace(binder.Password) ||
				binder.Password.Length < 8 ||
				binder.Password.Length > 256
			) {
				return BadRequest();
			}
			try {
				var salt = GenerateSalt();
				using (var db = new DbConnection()) {
					var userAccount = db.CreateUserAccount(binder.Name, binder.Email, HashPassword(binder.Password, salt), salt);
					await SignIn(userAccount.Id);
					return Json(userAccount);
				}
			} catch (Exception ex) {
				return BadRequest((ex as ValidationException)?.Errors);
			}
      }
		[HttpGet]
		public IActionResult GetUserAccount() {
			using (var db = new DbConnection()) {
				return Json(db.GetUserAccount(this.User.GetUserAccountId()));
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignIn([FromBody] SignInBinder binder) {
			using (var db = new DbConnection()) {
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
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SignOut() {
			await this.HttpContext.Authentication.SignOutAsync(Startup.AuthenticationScheme);
			return Ok();
		}
   }
}