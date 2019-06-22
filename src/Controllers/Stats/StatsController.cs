using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using api.DataAccess;
using api.Authentication;
using api.DataAccess.Models;
using System.Collections.Generic;
using System;
using api.Analytics;

namespace api.Controllers.Stats {
	public class StatsController : Controller {
		private DatabaseOptions dbOpts;
		public StatsController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		[HttpGet]
		public async Task<IActionResult> Leaderboards() {
			var userAccountId = User.GetUserAccountId();
			DateTime
				now = DateTime.UtcNow,
				longestReadSinceDate = now.AddDays(-7),
				scoutSinceDate = now.AddDays(-7),
				scribeSinceDate = now.AddDays(-7);
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					new {
						LongestRead = await db.GetLongestReadLeaderboard(
							maxCount: 10,
							sinceDate: longestReadSinceDate
						),
						ReadCount = await db.GetReadCountLeaderboard(
							maxCount: 10,
							sinceDate: null
						),
						Scout = await db.GetScoutLeaderboard(
							maxCount: 10,
							sinceDate: scoutSinceDate
						),
						Scribe = await db.GetScribeLeaderboard(
							maxCount: 10,
							sinceDate: scribeSinceDate
						),
						Streak = await db.GetCurrentStreakLeaderboard(
							userAccountId: userAccountId,
							maxCount: 10
						),
						UserRankings = await db.GetUserLeaderboardRankings(
							userAccountId: userAccountId,
							longestReadSinceDate: longestReadSinceDate,
							scoutSinceDate: scoutSinceDate,
							scribeSinceDate: scribeSinceDate
						),
						WeeklyReadCount = await db.GetReadCountLeaderboard(
							maxCount: 10,
							sinceDate: now.AddDays(-7)
						)
					}
				);
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
				var userReadCount = await db.GetUserReadCount(userAccountId: userAccountId);
				if (this.ClientVersionIsGreaterThanOrEqualTo(
					new Dictionary<ClientType, SemanticVersion>() {
						{ ClientType.WebAppServer, new SemanticVersion("1.4.0") },
						{ ClientType.WebAppClient, new SemanticVersion("1.4.0") }
					}
				)) {
					return Json(
						data: new {
							Rows = rows,
							UserReadCount = userReadCount
						}
					);
				} else {
					return Json(
						data: new {
							Rows = rows,
							UserStats = new {
								ReadCount = userReadCount
							}
						}
					);
				}
			}
		}
		[HttpGet]
		public async Task<IActionResult> UserCount() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					new {
						UserCount = await db.GetUserCount()
					}
				);
			}
		}
	}
}