using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class AuthController : Controller {
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppleWebResponse(
			[FromForm] AppleWebResponseForm form
		) {
			await System.IO.File.WriteAllTextAsync(
				path: @"logs\" + System.IO.Path.GetRandomFileName(),
				contents: (
					"code: " + form.code + "\n" +
					"id_token: " + form.id_token + "\n" +
					"state: " + form.state + "\n" +
					"user: " + form.user + "\n" +
					"error: " + form.error + "\n"
				)
			);
			return Redirect("https://readup.com/?apple-sign-in=success");
		}
	}
}