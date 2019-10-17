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

namespace api.Controllers.Analytics {
	[AuthorizeUserAccountRole(UserAccountRole.Admin)]
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
					analytics: this.GetRequestAnalytics()
				);
				return Ok();
			}
		}
		public async Task<JsonResult> KeyMetrics(DateTime startDate, DateTime endDate) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetKeyMetrics(startDate, endDate));
			}
		}
		public async Task<JsonResult> UserAccountCreations(DateTime startDate, DateTime endDate) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetUserAccountCreations(startDate, endDate));
			}
		}
	}
}