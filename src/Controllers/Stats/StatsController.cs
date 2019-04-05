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
		public async Task<IActionResult> Leaderboards() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(new {
					CurrentStreak = await db.GetCurrentStreakLeaderboard(
						userAccountId: this.User.GetUserAccountId(),
						maxCount: 10
					),
					ReadCount = await db.GetReadCountLeaderboard(10)
				});
			}
		}
		[HttpGet]
		public async Task<IActionResult> UserStats() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetUserStats(this.User.GetUserAccountId()));
			}
		}
	}
}