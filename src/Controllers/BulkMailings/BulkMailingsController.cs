using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Authentication;
using api.Authorization;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.BulkMailings {
	[AuthorizeUserAccountRole(UserAccountRole.Admin)]
	public class BulkMailingsController : Controller {
		private DatabaseOptions dbOpts;
		private static string websiteUpdatesList = "WebsiteUpdates";
		private static string suggestedReadingsList = "SuggestedReadings";
		private static Dictionary<string, string> listDescriptions = new Dictionary<string, string>() {
			{ websiteUpdatesList, "website updates" },
			{ suggestedReadingsList, "suggested readings" }
		};
		public BulkMailingsController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		private IEnumerable<UserAccount> GetMailableUsers(EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return db
				.ListUserAccounts()
				.Where(account => emailService.CanSendEmailTo(account))
				.ToArray();
			}
		}
		[HttpGet]
		public JsonResult List() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.ListBulkMailings().OrderByDescending(m => m.DateSent));
			}
		}
		[HttpGet]
		public JsonResult Lists([FromServices] EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				emailService.RegisterEmailBounces(db.ListEmailBounces());
				var mailableUsers = GetMailableUsers(emailService);
				return Json(new[] {
					new KeyValuePair<string, string>($"{websiteUpdatesList} ({mailableUsers.Count(u => u.ReceiveWebsiteUpdates)})", websiteUpdatesList),
					new KeyValuePair<string, string>($"{suggestedReadingsList} ({mailableUsers.Count(u => u.ReceiveSuggestedReadings)})", suggestedReadingsList)
				});
			}
		}
		[HttpPost]
		public async Task<IActionResult> SendTest([FromBody] BulkMailingTestBinder binder, [FromServices] EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				emailService.RegisterEmailBounces(db.ListEmailBounces());
				try {
					var isSuccessful = await emailService.SendBulkMailingEmail(
						recipient: new UserAccount() {
							Name = "Test User",
							Email = binder.EmailAddress,
							IsEmailConfirmed = true
						},
						list: binder.List,
						subject: binder.Subject,
						body: binder.Body,
						listDescription: listDescriptions[binder.List]
					);
					if (isSuccessful) {
						return Ok();
					}
					return BadRequest();
				} catch {
					return BadRequest();
				}
			}
		}
		[HttpPost]
		public async Task<IActionResult> Send([FromBody] BulkMailingBinder binder, [FromServices] EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				emailService.RegisterEmailBounces(db.ListEmailBounces());
				IEnumerable<UserAccount> recipients;
				if (binder.List == websiteUpdatesList) {
					recipients = GetMailableUsers(emailService)
						.Where(u => u.ReceiveWebsiteUpdates)
						.ToArray();
				} else if (binder.List == suggestedReadingsList) {
					recipients = GetMailableUsers(emailService)
						.Where(u => u.ReceiveSuggestedReadings)
						.ToArray();
				} else {
					return BadRequest();
				}
				var bulkMailingRecipients = new List<CreateBulkMailingRecipient>();
				foreach (var recipient in recipients) {
					var bulkMailingRecipient = new CreateBulkMailingRecipient() {
						UserAccountId = recipient.Id
					};
					try {
						var isSuccessful = await emailService.SendBulkMailingEmail(
							recipient: recipient,
							list: binder.List,
							subject: binder.Subject,
							body: binder.Body,
							listDescription: listDescriptions[binder.List]
						);
						bulkMailingRecipient.IsSuccessful = isSuccessful;
					} catch {
						bulkMailingRecipient.IsSuccessful = false;
					}
					bulkMailingRecipients.Add(bulkMailingRecipient);
				}
				db.CreateBulkMailing(
					subject: binder.Subject,
					body: binder.Body,
					list: binder.List,
					userAccountId: User.GetUserAccountId(),
					recipients: bulkMailingRecipients.ToArray()
				);
			}
			return Ok();
		}
	}
}