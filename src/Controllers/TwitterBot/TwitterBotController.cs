using System.Linq;
using System.Threading.Tasks;
using api.Authentication;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.TwitterBot {
	public class TwitterBotController : Controller {
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> TweetAotd(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromServices] IOptions<DatabaseOptions> databaseOptions,
			[FromServices] TwitterAuthService twitterAuth,
			[FromForm] AotdForm form
		) {
			if (form.ApiKey == authOptions.Value.ApiKey) {
				Article aotd;
				using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
					aotd = (await db.GetAotds(dayCount: 1)).Single();
				}
				twitterAuth.TweetAotd(aotd);
			}
			return BadRequest();
		}
	}
}