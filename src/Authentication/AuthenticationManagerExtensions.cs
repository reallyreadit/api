using System.Security.Claims;
using System.Threading.Tasks;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;

namespace api.Authentication {
	public static class AuthenticationManagerExtensions {
		public static async Task SignInAsync(this AuthenticationManager authManager, string authenticationScheme, UserAccount userAccount) {
			var principal = new ClaimsPrincipal(new[] {
				new ClaimsIdentity(
					claims: new[] {
						new Claim(ClaimTypes.NameIdentifier, userAccount.Id.ToString()),
						new Claim(ClaimTypes.Role, userAccount.Role.ToString())
					},
					authenticationType: "ApplicationCookie"
				)
			});
			await authManager.SignInAsync(
				authenticationScheme: authenticationScheme,
				principal: principal,
				properties: new AuthenticationProperties() {
					IsPersistent = true
				}
			);
			authManager.HttpContext.User = principal;
		}
	}
}