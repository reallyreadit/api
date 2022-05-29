// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> AuthorLeaderboards(
			[FromQuery] AuthorLeaderboardsRequest request
		) {
			int maxRank;
			DateTime? sinceDate;
			var now = DateTime.UtcNow;
			switch (request.TimeWindow) {
				case AuthorLeaderboardsTimeWindow.PastWeek:
					maxRank = 25;
					sinceDate = now.Subtract(TimeSpan.FromDays(7));
					break;
				case AuthorLeaderboardsTimeWindow.PastMonth:
					maxRank = 50;
					sinceDate = now.Subtract(TimeSpan.FromDays(30));
					break;
				case AuthorLeaderboardsTimeWindow.PastYear:
					maxRank = 100;
					sinceDate = now.Subtract(TimeSpan.FromDays(365));
					break;
				case AuthorLeaderboardsTimeWindow.AllTime:
					maxRank = 100;
					sinceDate = null;
					break;
				default:
					throw new ArgumentException("Unexpected time window.");
			}
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await db.GetAuthorLeaderboard(maxRank, sinceDate)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Leaderboards() {
			using (var db = new NpgsqlConnection(
				connectionString: dbOpts.ConnectionString
			)) {
				var now = DateTime.UtcNow;
				var userAccountId = User.GetUserAccountIdOrDefault();
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
							UserRankings = (
								userAccountId.HasValue ?
									await db.GetUserLeaderboardRankings(
										userAccountId: userAccountId.Value,
										now: now
									) :
									null
							),
							WeeklyReadCount = leaderboards.WeeklyReadCount,
							TimeZoneName = (
								userAccountId.HasValue ?
									(await db.GetTimeZoneById(
										id: (await db.GetUserAccountById(
											userAccountId: userAccountId.Value
										))
										.TimeZoneId
										.Value
									))
									.Name :
									null
							)
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