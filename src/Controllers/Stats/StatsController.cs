using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using api.DataAccess;
using api.Authentication;
using api.DataAccess.Models;
using System.Collections.Generic;
using System;

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
		[HttpGet]
		public async Task<IActionResult> ReadingTime(ReadingTimeTotalsTimeWindow timeWindow) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				IEnumerable<ReadingTimeTotalsRow> rows;
				switch (timeWindow) {
					case ReadingTimeTotalsTimeWindow.AllTime:
						rows = await db.GetMonthlyReadingTimeTotals(
							userAccountId: userAccountId,
							numberOfMonths: null
						);
						break;
					case ReadingTimeTotalsTimeWindow.PastWeek:
						rows = await db.GetDailyReadingTimeTotals(
							userAccountId: userAccountId,
							numberOfDays: 6
						);
						break;
					case ReadingTimeTotalsTimeWindow.PastMonth:
						rows = await db.GetDailyReadingTimeTotals(
							userAccountId: userAccountId,
							numberOfDays: 29
						);
						break;
					case ReadingTimeTotalsTimeWindow.PastYear:
						rows = await db.GetMonthlyReadingTimeTotals(
							userAccountId: userAccountId,
							numberOfMonths: 11
						);
						break;
					default:
						throw new ArgumentException($"Unexpected value for {nameof(timeWindow)}");
				}
				return Json(
					new {
						Rows = rows,
						UserStats = await db.GetUserStats(userAccountId)
					}
				);
			}
		}
	}
}