using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using api.DataAccess;
using System;
using Microsoft.AspNetCore.Http;
using System.Linq;

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
		private void SetSessionKeyCookie(byte[] sessionKey) {
			Response.Cookies.Append(
				key: "sessionKey",
				value: Convert.ToBase64String(sessionKey),
				options: new CookieOptions() {
					Domain = Startup.CookieDomain,
					HttpOnly = true
				}
			);
		}
		[HttpPost]
        public IActionResult CreateAccount([FromBody] CreateAccountBinder binder) {
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
					SetSessionKeyCookie(db.CreateSession(userAccount.Id).Id);
					return Json(userAccount);
				}
			} catch (Exception ex) {
				return BadRequest((ex as ValidationException)?.Errors);
			}
        }
		[HttpGet]
		public IActionResult GetUserAccount() {
			using (var db = new DbConnection()) {
				return Json(db.GetUserAccount(db.GetSession(this.GetSessionKey()).UserAccountId));
			}
		}
		[HttpPost]
		public IActionResult SignIn([FromBody] SignInBinder binder) {
			using (var db = new DbConnection()) {
				var userAccount = db.FindUserAccount(binder.Name);
				if (userAccount == null) {
					return BadRequest(new[] { "UserAccountNotFound" });
				}
				if (!userAccount.PasswordHash.SequenceEqual(HashPassword(binder.Password, userAccount.PasswordSalt))) {
					return BadRequest(new[] { "IncorrectPassword" });
				}
				SetSessionKeyCookie(db.CreateSession(userAccount.Id).Id);
				return Json(userAccount);
			}
		}
		[HttpPost]
		public IActionResult SignOut() {
			using (var db = new DbConnection()) {
				db.EndSession(this.GetSessionKey());
			}
			Response.Cookies.Delete("sessionKey", new CookieOptions() {
				Domain = Startup.CookieDomain
			});
			return Ok();
		}
    }
}