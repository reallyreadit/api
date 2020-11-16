using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Subscriptions {
	public class SubscriptionsController : Controller {
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppStoreNotification() {
			using (
				var body = new StreamReader(Request.Body)
			) {
				await System.IO.File.WriteAllTextAsync(
					path: @"logs/iap-" + Path.GetRandomFileName(),
					contents: "Date: " + DateTime.UtcNow.ToString("s") + "\nContent-Type: " + Request.ContentType + "\nBody:\n" + (await body.ReadToEndAsync())
				);
			}
			return Ok();
		}
	}
}