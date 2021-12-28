using System.Collections.Generic;
using System.Linq;
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
using api.Authorization;
using System;

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
		[HttpPost]
		public async Task<ActionResult<ContactStatusResponse>> ContactStatus(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromBody] ContactStatusRequest request
		) {
			if (request.ApiKey != authOptions.Value.ApiKey) {
				return BadRequest();
			}
			IEnumerable<long> updatedAuthorIds;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				updatedAuthorIds = await db.AssignContactStatusToAuthorsAsync(
					request.Assignments
						.Select(
							assignment => new AuthorContactStatusAssignment(
								slug: assignment.Slug,
								contactStatus: assignment.Status
							)
						)
						.ToArray()
				);
			}
			return new ContactStatusResponse(
				updatedRecordCount: updatedAuthorIds.Count()
			);
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
						totalPayouts: 0,
						userName: linkedUserAccount?.Name,
						donationRecipient: donationRecipient
					)
				);
			}
		}
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		[HttpPost]
		public async Task<IActionResult> UserAccountAssignment(
			[FromBody] AuthorUserAccountAssignmentRequest request
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// Look up the author first.
				var author = await db.GetAuthor(slug: request.AuthorSlug);
				if (author == null) {
					return Problem("Author not found.", statusCode: 404);
				}
				// Look up the user account.
				var user = await db.GetUserAccountByName(userName: request.UserAccountName);
				if (user == null) {
					return Problem("User not found.", statusCode: 404);
				}
				// Verify that the author isn't already associated with another user account.
				if (author.UserAccountId.HasValue) {
					return Problem("Author has already been verified.", statusCode: 400);
				}
				// Attempt the assignment.
				try {
					author = await db.AssignUserAccountToAuthor(
						authorId: author.Id,
						userAccountId: user.Id
					);
					return Ok();
				} catch (Exception ex) {
					log.LogError(ex, "Failed to assign user with ID {UserId} to author with ID {AuthorId}.", user.Id, author.Id);
					return Problem("Assignment failed.", statusCode: 500);
				}
			}
		}
	}
}