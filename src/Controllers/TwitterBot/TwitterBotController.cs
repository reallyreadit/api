using System.Linq;
using System.Threading.Tasks;
using api.Authentication;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Messaging;
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
			[FromServices] EmailService emailService,
			[FromForm] AotdForm form
		) {
			if (form.ApiKey != authOptions.Value.ApiKey) {
				return BadRequest();
			}
			Article aotd;
			using (var db = new NpgsqlConnection(databaseOptions.Value.ConnectionString)) {
				aotd = await db.GetArticleById(
					articleId: (await db.GetAotds(dayCount: 1)).Single(),
					userAccountId: null
				);
			}
			var tweetText = await twitterAuth.GetAotdTweetText(aotd);
			await emailService.Send(
				new EmailMessage(
					from: new EmailMailbox("AOTD Bot", "support@readup.com"),
					replyTo: null,
					to: new EmailMailbox("Bill Loundy", "bill@readup.com"),
					subject: "AOTD Tweet",
					body: tweetText
				),
				new EmailMessage(
					from: new EmailMailbox("AOTD Bot", "support@readup.com"),
					replyTo: null,
					to: new EmailMailbox("Thor Galle", "thor@readup.com"),
					subject: "AOTD Tweet",
					body: tweetText
				)
			);
			return Ok();
		}
	}
}