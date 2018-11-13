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
		public async Task<IActionResult> Reading() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetUserReadStats(this.User.GetUserAccountId()));
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> ReadingLeaderboards() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetReadCountLeaderboard(10));
			}
		}
	}
}