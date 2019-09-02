using System;
using System.Threading.Tasks;
using api.DataAccess.Models;
using Npgsql;

namespace api.DataAccess.Stats {
	public static class StatsQueries {
		public static async Task<Leaderboards> GetLeaderboards(
			this NpgsqlConnection db,
			long userAccountId,
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