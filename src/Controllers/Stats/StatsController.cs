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
using api.DataAccess.Stats;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers.Stats {
	public class StatsController : Controller {
		private DatabaseOptions dbOpts;
		public StatsController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		[HttpGet]
		public async Task<IActionResult> Leaderboards() {
			using (var db = new NpgsqlConnection(
				connectionString: dbOpts.ConnectionString
			)) {
				var now = DateTime.UtcNow;
				var userAccountId = User.GetUserAccountId();
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: now
				);
				if (this.ClientVersionIsGreaterThanOrEqualTo(
					new Dictionary<ClientType, SemanticVersion>() {
						{ ClientType.WebAppServer, new SemanticVersion("1.4.0") },
						{ ClientType.WebAppClient, new SemanticVersion("1.4.0") }
					}
				)) {
					return Json(
						data: new {
							LongestRead = leaderboards.LongestRead,
							ReadCount = leaderboards.ReadCount,
							Scout = leaderboards.Scout,
							Scribe = leaderboards.Scribe,
							Streak = leaderboards.Streak,
							UserRankings = await db.GetUserLeaderboardRankings(
								userAccountId: userAccountId,
								now: now
							),
							WeeklyReadCount = leaderboards.WeeklyReadCount,
							TimeZoneName = (await db.GetTimeZoneById(
									id: (await db.GetUserAccountById(
										userAccountId: userAccountId
									))
									.TimeZoneId
									.Value
								))
								.Name
						}
					);
				} else {
					return Json(
						data: new {
							CurrentStreak = leaderboards.Streak
								.OrderBy(ranking => ranking.Rank)
								.ThenBy(ranking => ranking.UserName)
								.Take(10)
								.Select(ranking => new {
									Name = ranking.UserName,
									Streak = ranking.Score
								}),
							ReadCount = leaderboards.ReadCount
								.OrderBy(ranking => ranking.Rank)
								.ThenBy(ranking => ranking.UserName)
								.Take(10)
								.Select(ranking => new {
									Name = ranking.UserName,
									ReadCount = ranking.Score
								})
						}
					);
				}
			}
		}
		// deprecated as of WebApp 1.4.0
		[HttpGet]
		public async Task<IActionResult> UserStats() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userRankings = await db.GetUserLeaderboardRankings(
					userAccountId: User.GetUserAccountId()
				);
				return Json(
					data: new {
						ReadCount = userRankings.ReadCount.Score,
						ReadCountRank = userRankings.ReadCount.Rank,
						Streak = (
							userRankings.Streak.DayCount > 0 ?
								new Nullable<int>(userRankings.Streak.DayCount + (!userRankings.Streak.IncludesToday ? 1 : 0)) :
								null
						),
						StreakRank = (
							userRankings.Streak.DayCount > 0 ?
								new Nullable<int>(userRankings.Streak.Rank) :
								null
						),
						UserCount = await db.GetUserCount()
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
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> UserCount() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					data: new {
						UserCount = await db.GetUserCount()
					}
				);
			}
		}
	}
}