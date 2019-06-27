using System;
using System.Threading.Tasks;
using Npgsql;

namespace api.DataAccess.Stats {
	public static class StatsQueries {
		public static TimeSpan LongestReadOffset => TimeSpan.FromDays(30);
		public static TimeSpan ScoutOffset => TimeSpan.FromDays(30);
		public static TimeSpan ScribeOffset => TimeSpan.FromDays(30);
		public static async Task<Leaderboards> GetLeaderboards(
			this NpgsqlConnection db,
			long userAccountId,
			DateTime now
		) {
			return new Leaderboards() {
				LongestRead = await db.GetLongestReadLeaderboard(
					maxRank: 5,
					sinceDate: now.Subtract(LongestReadOffset)
				),
				ReadCount = await db.GetReadCountLeaderboard(
					maxRank: 5,
					sinceDate: null
				),
				Scout = await db.GetScoutLeaderboard(
					maxRank: 5,
					sinceDate: now.Subtract(ScoutOffset)
				),
				Scribe = await db.GetScribeLeaderboard(
					maxRank: 5,
					sinceDate: now.Subtract(ScribeOffset)
				),
				Streak = await db.GetCurrentStreakLeaderboard(
					userAccountId: userAccountId,
					maxRank: 5
				),
				WeeklyReadCount = await db.GetReadCountLeaderboard(
					maxRank: 5,
					sinceDate: now.AddDays(-7)
				)
			};
		}
	}
}