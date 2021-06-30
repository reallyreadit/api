using System.Threading.Tasks;
using api.Configuration;
using api.Controllers.Shared;
using api.DataAccess;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.AuthorsController {
	public class AuthorsController : Controller {
		private readonly DatabaseOptions databaseOptions;
		private readonly ILogger<AuthorsController> log;
		public AuthorsController(
			IOptions<DatabaseOptions> databaseOptions,
			ILogger<AuthorsController> log
		) {
			this.databaseOptions = databaseOptions.Value;
			this.log = log;
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Profile(
			[FromQuery] AuthorProfileRequest request
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				var author = await db.GetAuthor(request.Slug);
				if (author == null) {
					log.LogError("Author lookup failed. Slug: {Slug}", request.Slug);
					return BadRequest(
						new[] { "Author not found." }
					);
				}
				var linkedUserAccount = await db.GetUserAccountByAuthorSlug(
					authorSlug: author.Slug
				);
				DonationRecipient donationRecipient;
				if (linkedUserAccount == null) {
					donationRecipient = await db.GetDonationRecipientForAuthorAsync(authorId: author.Id);
				} else {
					donationRecipient = null;
				}
				var distributionReport = await db.RunAuthorDistributionReportForSubscriptionPeriodDistributionsAsync(
					authorId: author.Id
				);
				return Json(
					new AuthorProfileClientModel(
						name: author.Name,
						slug: author.Slug,
						totalEarnings: distributionReport.Amount,
						userName: linkedUserAccount?.Name,
						donationRecipient: donationRecipient
					)
				);
			}
		}
	}
}