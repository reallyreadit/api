using System.Security.Claims;
using System.Threading.Tasks;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace api.Authentication {
	public static class HttpContextExtensions {
		public static async Task SignInAsync(
			this HttpContext httpContext,
			UserAccount userAccount
		) {
			var principal = new ClaimsPrincipal(new[] {
				new ClaimsIdentity(
					claims: new[] {
						new Claim(ClaimTypes.NameIdentifier, userAccount.Id.ToString()),
						new Claim(ClaimTypes.Role, userAccount.Role.ToString())
					},
					authenticationType: "ApplicationCookie"
				)
			});
			await httpContext.SignInAsync(
				principal: principal,
				properties: new AuthenticationProperties() {
					IsPersistent = true
				}
			);
			httpContext.User = principal;
		}
	}
}