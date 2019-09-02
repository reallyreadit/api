using System.Collections.Generic;
using api.DataAccess.Stats;

namespace api.DataAccess.Models {
	public class UserLeaderboardRankings {
		public Ranking LongestRead { get; set; }
		public Ranking ReadCount { get; set; }
		public Ranking ScoutCount { get; set; }
		public Ranking ScribeCount { get; set; }
		public StreakRanking Streak { get; set; }
		public Ranking WeeklyReadCount { get; set; }
		public LeaderboardBadge GetBadge() {
			var badge = LeaderboardBadge.None;
			foreach (
				 var kvp in new Dictionary<LeaderboardBadge, IRanking>() {
					{ LeaderboardBadge.ReadCount, ReadCount },
					{ LeaderboardBadge.Scout, ScoutCount },
					{ LeaderboardBadge.Scribe, ScribeCount },
					{ LeaderboardBadge.Streak, Streak },
					{ LeaderboardBadge.WeeklyReadCount, WeeklyReadCount }
				}
			) {
				if (kvp.Value.Rank != 0 && kvp.Value.Rank <= Leaderboards.MaxRank) {
					badge |= kvp.Key;
				}
			}
			return badge;
		}
	}
}