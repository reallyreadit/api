using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.HealthCheck {
	public class HealthCheckController : Controller {
		[AllowAnonymous]
		public IActionResult Check() => Ok();
	}
}