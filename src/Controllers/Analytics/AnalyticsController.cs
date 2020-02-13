using System;
using System.Threading.Tasks;
using api.Authorization;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using Microsoft.AspNetCore.Authorization;
using api.Analytics;
using System.IO;
using api.Authentication;

namespace api.Controllers.Analytics {
	public class AnalyticsController: Controller {
		private DatabaseOptions dbOpts;
		public AnalyticsController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> ClientErrorReport() {
			using (var bodyReader = new StreamReader(Request.Body))
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await db.LogClientErrorReport(
					content: await bodyReader.ReadToEndAsync(),
					analytics: this.GetClientAnalytics()
				);
				return Ok();
			}
		}
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		public async Task<JsonResult> KeyMetrics(DateTime startDate, DateTime endDate) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetKeyMetrics(startDate, endDate));
			}
		}
		[HttpPost]
		public async Task<IActionResult> Orientation(
			[FromBody] OrientationForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await db.LogOrientationAnalytics(
					userAccountId: User.GetUserAccountId(),
					trackingPlayCount: form.TrackingPlayCount,
					trackingSkipped: form.TrackingSkipped,
					trackingDuration: form.TrackingDuration,
					importPlayCount: form.ImportPlayCount,
					importSkipped: form.ImportSkipped,
					importDuration: form.ImportDuration,
					notificationsResult: form.NotificationsResult,
					notificationsSkipped: form.NotificationsSkipped,
					notificationsDuration: form.NotificationsDuration,
					shareResultId: form.ShareResultId,
					shareSkipped: form.ShareSkipped,
					shareDuration: form.ShareDuration
				);
			}
			return Ok();
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Share(
			[FromBody] ShareForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await db.LogShareResult(
					id: form.Id ?? Guid.NewGuid(),
					clientType: this.GetClientAnalytics().Type,
					userAccountId: User.GetUserAccountIdOrDefault(),
					action: form.Action,
					activityType: form.ActivityType,
					completed: form.Completed,
					error: form.Error
				);
			}
			return Ok();
		}
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		public async Task<JsonResult> UserAccountCreations(DateTime startDate, DateTime endDate) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetUserAccountCreations(startDate, endDate));
			}
		}
	}
}