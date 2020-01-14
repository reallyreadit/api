using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class AuthController : Controller {
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppleWeb(
			[FromForm] AppleWebResponseForm form
		) {
			await System.IO.File.WriteAllTextAsync(
				path: @"logs\" + System.IO.Path.GetRandomFileName(),
				contents: (
					"code=" + WebUtility.UrlEncode(form.code) + "&" +
					"id_token=" + WebUtility.UrlEncode(form.id_token) + "&" +
					"state=" + WebUtility.UrlEncode(form.state) + "&" +
					"user=" + WebUtility.UrlEncode(form.user)
				)
			);
			return Redirect("https://readup.com/");
		}
	}
}