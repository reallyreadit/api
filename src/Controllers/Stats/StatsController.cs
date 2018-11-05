using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using api.DataAccess;
using api.Authentication;

namespace api.Controllers.Stats {
	public class StatsController : Controller {
		private DatabaseOptions dbOpts;
		public StatsController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		[HttpGet]
		public async Task<IActionResult> WeeklyReading() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetUserWeeklyReadStats(this.User.GetUserAccountId()));
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> WeeklyReadingLeaderboards() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(new {
					Reads = await db.GetWeeklyReadCountLeaderboard(10),
					Words = await db.GetWeeklyWordCountLeaderboard(10)
				});
			}
		}
	}
}