using System;
using System.Threading.Tasks;
using api.Authorization;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.Analytics {
	[AuthorizeUserAccountRole(UserAccountRole.Admin)]
	public class AnalyticsController: Controller {
		private DatabaseOptions dbOpts;
		public AnalyticsController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		public async Task<JsonResult> KeyMetrics(DateTime startDate, DateTime endDate) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetKeyMetrics(startDate, endDate));
			}
		}
	}
}