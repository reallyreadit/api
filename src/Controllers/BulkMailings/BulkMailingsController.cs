using System;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.BulkMailings {
	[AuthorizeUserAccountRole(UserAccountRole.Admin)]
	public class BulkMailingsController : Controller {
		private DatabaseOptions dbOpts;
		private readonly EmailService emailService;
		private readonly ILogger<BulkMailingsController> log;
		private const string websiteUpdatesList = "WebsiteUpdates";
		private const string suggestedReadingsList = "SuggestedReadings";
		private const string confirmationReminderList = "ConfirmationReminder";
		private static Dictionary<string, string> listDescriptions = new Dictionary<string, string>() {
			{ websiteUpdatesList, "community updates" },
			{ suggestedReadingsList, "suggested readings" }
		};
		public BulkMailingsController(
			IOptions<DatabaseOptions> dbOpts,
			EmailService emailService,
			ILogger<BulkMailingsController> log
		) {
			this.dbOpts = dbOpts.Value;
			this.emailService = emailService;
			this.log = log;
		}
		private IEnumerable<UserAccount> GetWebsiteUpdateListUsers(NpgsqlConnection db) => (
			db
				.ListUserAccounts()
				.Where(user =>
					user.IsEmailConfirmed &&
					user.ReceiveWebsiteUpdates &&
					!emailService.HasEmailAddressBounced(user.Email)
				)
				.ToArray()
		);
		private IEnumerable<UserAccount> GetSuggestedReadingListUsers(NpgsqlConnection db) => (
			db
				.ListUserAccounts()
				.Where(user =>
					user.IsEmailConfirmed &&
					user.ReceiveSuggestedReadings &&
					!emailService.HasEmailAddressBounced(user.Email)
				)
				.ToArray()
		);
		private IEnumerable<UserAccount> GetConfirmationReminderListUsers(NpgsqlConnection db) {
			var reminderRecipientAddresses = db
				.ListConfirmationReminderRecipients()
				.Select(user => EmailService.NormalizeEmailAddress(user.Email))
				.ToArray();
			return db
				.ListUserAccounts()
				.Where(user =>
					!user.IsEmailConfirmed &&
					!reminderRecipientAddresses.Contains(EmailService.NormalizeEmailAddress(user.Email)) &&
					!emailService.HasEmailAddressBounced(user.Email)
				)
				.ToArray();
		}
		[HttpGet]
		public JsonResult List() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.ListBulkMailings().OrderByDescending(m => m.DateSent));
			}
		}
		[HttpGet]
		public JsonResult Lists() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(new[] {
					new KeyValuePair<string, string>($"{websiteUpdatesList} ({GetWebsiteUpdateListUsers(db).Count()})", websiteUpdatesList),
					new KeyValuePair<string, string>($"{suggestedReadingsList} ({GetSuggestedReadingListUsers(db).Count()})", suggestedReadingsList),
					new KeyValuePair<string, string>($"{confirmationReminderList} ({GetConfirmationReminderListUsers(db).Count()})", confirmationReminderList)
				});
			}
		}
		[HttpPost]
		public async Task<IActionResult> SendTest([FromBody] BulkMailingTestBinder binder, [FromServices] EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				try {
					bool isSuccessful;
					if (binder.List == confirmationReminderList) {
						isSuccessful = await emailService.SendConfirmationReminderEmail(
							recipient: new UserAccount() {
								Name = "Test User",
								Email = binder.EmailAddress,
								IsEmailConfirmed = false
							},
							subject: binder.Subject,
							body: binder.Body,
							emailConfirmationId: 0
						);
					} else {
						isSuccessful = await emailService.SendListSubscriptionEmail(
							recipient: new UserAccount() {
								Name = "Test User",
								Email = binder.EmailAddress,
								IsEmailConfirmed = true
							},
							subject: binder.Subject,
							body: binder.Body,
							listDescription: listDescriptions[binder.List]
						);
					}
					if (isSuccessful) {
						return Ok();
					}
					return BadRequest();
				} catch (Exception ex) {
					log.LogError(500, ex, "Error sending bulk email.");
					return BadRequest();
				}
			}
		}
		[HttpPost]
		public async Task<IActionResult> Send([FromBody] BulkMailingBinder binder, [FromServices] EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				IEnumerable<UserAccount> recipients;
				switch (binder.List) {
					case websiteUpdatesList:
						recipients = GetWebsiteUpdateListUsers(db);
						break;
					case suggestedReadingsList:
						recipients = GetSuggestedReadingListUsers(db);
						break;
					case confirmationReminderList:
						recipients = GetConfirmationReminderListUsers(db);
						break;
					default:
						return BadRequest();
				}
				var bulkMailingRecipients = new List<BulkMailingRecipient>();
				foreach (var recipient in recipients) {
					var bulkMailingRecipient = new BulkMailingRecipient() {
						UserAccountId = recipient.Id
					};
					try {
						if (binder.List == confirmationReminderList) {
							bulkMailingRecipient.IsSuccessful = await emailService.SendConfirmationReminderEmail(
								recipient: recipient,
								subject: binder.Subject,
								body: binder.Body,
								emailConfirmationId: db.GetLatestUnconfirmedEmailConfirmation(recipient.Id).Id
							);
						} else {
							bulkMailingRecipient.IsSuccessful = await emailService.SendListSubscriptionEmail(
								recipient: recipient,
								subject: binder.Subject,
								body: binder.Body,
								listDescription: listDescriptions[binder.List]
							);
						}
					} catch {
						bulkMailingRecipient.IsSuccessful = false;
					}
					bulkMailingRecipients.Add(bulkMailingRecipient);
				}
				// temp workaround to circumvent npgsql type mapping bug
				db.CreateBulkMailing(
					subject: binder.Subject,
					body: binder.Body,
					list: binder.List,
					userAccountId: User.GetUserAccountId(db),
					recipientIds: bulkMailingRecipients.Select(recipient => recipient.UserAccountId).ToArray(),
					recipientResults: bulkMailingRecipients.Select(recipient => recipient.IsSuccessful).ToArray()
				);
			}
			return Ok();
		}
	}
}