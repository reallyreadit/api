using System;
using System.Linq;
using System.Threading.Tasks;
using api.Authentication;
using api.Authorization;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Messaging;
using api.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.BulkMailings {
	[AuthorizeUserAccountRole(UserAccountRole.Admin)]
	public class BulkMailingsController : Controller {
		private DatabaseOptions dbOpts;
		private readonly ILogger<BulkMailingsController> log;
		public BulkMailingsController(
			IOptions<DatabaseOptions> dbOpts,
			EmailService emailService,
			ILogger<BulkMailingsController> log
		) {
			this.dbOpts = dbOpts.Value;
			this.log = log;
		}
		[HttpGet]
		public JsonResult List() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.GetBulkMailings().OrderByDescending(m => m.DateSent).Take(7));
			}
		}
		[HttpPost]
		public async Task<IActionResult> SendTest(
			[FromServices] NotificationService notificationService,
			[FromBody] BulkMailingTestBinder binder
		) {
			try {
				await notificationService.CreateCompanyUpdateNotifications(
					authorId: User.GetUserAccountId(),
					subject: binder.Subject,
					body: binder.Body,
					subscriptionStatusFilter: binder.SubscriptionStatusFilter,
					freeForLifeFilter: binder.FreeForLifeFilter,
					userCreatedAfterFilter: binder.UserCreatedAfterFilter,
					userCreatedBeforeFilter: binder.UserCreatedBeforeFilter,
					testEmailAddress: binder.EmailAddress
				);
				return Ok();
			} catch (Exception ex) {
				log.LogError(ex, "Error sending test bulk email.");
				return BadRequest(new[] { ex.Message });
			}
		}
		[HttpPost]
		public async Task<IActionResult> Send(
			[FromServices] NotificationService notificationService,
			[FromBody] BulkMailingBinder binder
		) {
			try {
				await notificationService.CreateCompanyUpdateNotifications(
					authorId: User.GetUserAccountId(),
					subject: binder.Subject,
					body: binder.Body,
					subscriptionStatusFilter: binder.SubscriptionStatusFilter,
					freeForLifeFilter: binder.FreeForLifeFilter,
					userCreatedAfterFilter: binder.UserCreatedAfterFilter,
					userCreatedBeforeFilter: binder.UserCreatedBeforeFilter,
					testEmailAddress: null
				);
				return Ok();
			} catch (Exception ex) {
				log.LogError(ex, "Error sending bulk email.");
				return BadRequest(new[] { ex.Message });
			}
		}
	}
}