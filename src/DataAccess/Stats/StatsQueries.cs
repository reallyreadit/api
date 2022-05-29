// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;
using api.DataAccess.Models;
using Npgsql;

namespace api.DataAccess.Stats {
	public static class StatsQueries {
		public static async Task<Leaderboards> GetLeaderboards(
			this NpgsqlConnection db,
			long? userAccountId,
			DateTime now
		) {
			return new Leaderboards() {
				LongestRead = new LeaderboardRanking[0],
				ReadCount = await db.GetReadCountLeaderboard(
					maxRank: Leaderboards.MaxRank,
					sinceDate: null
				),
				Scout = await db.GetScoutLeaderboard(
					maxRank: Leaderboards.MaxRank,
					sinceDate: now.Subtract(Leaderboards.ScoutOffset)
				),
				Scribe = await db.GetScribeLeaderboard(
					maxRank: Leaderboards.MaxRank,
					sinceDate: now.Subtract(Leaderboards.ScribeOffset)
				),
				Streak = await db.GetCurrentStreakLeaderboard(
					userAccountId: userAccountId,
					maxRank: Leaderboards.MaxRank
				),
				WeeklyReadCount = await db.GetReadCountLeaderboard(
					maxRank: Leaderboards.MaxRank,
					sinceDate: now.AddDays(-7)
				)
			};
		}
	}
}