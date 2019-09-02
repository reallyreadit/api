using System;
using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.DataAccess.Stats {
	public class Leaderboards {
		public static TimeSpan LongestReadOffset => TimeSpan.FromDays(30);
		public static TimeSpan ScoutOffset => TimeSpan.FromDays(30);
		public static TimeSpan ScribeOffset => TimeSpan.FromDays(30);
		public static int MaxRank => 5;
		public IEnumerable<LeaderboardRanking> LongestRead { get; set; }
		public IEnumerable<LeaderboardRanking> ReadCount { get; set; }
		public IEnumerable<LeaderboardRanking> Scout { get; set; }
		public IEnumerable<LeaderboardRanking> Scribe { get; set; }
		public IEnumerable<LeaderboardRanking> Streak { get; set; }
		public IEnumerable<LeaderboardRanking> WeeklyReadCount { get; set; }
		public LeaderboardBadge GetBadge(
			string userName
		) {
			var badge = LeaderboardBadge.None;
			foreach (
				var kvp in new Dictionary<LeaderboardBadge, IEnumerable<LeaderboardRanking>>() {
					{ LeaderboardBadge.ReadCount, ReadCount },
					{ LeaderboardBadge.Scout, Scout },
					{ LeaderboardBadge.Scribe, Scribe },
					{ LeaderboardBadge.Streak, Streak },
					{ LeaderboardBadge.WeeklyReadCount, WeeklyReadCount }
				}
			) {
				foreach (var ranking in kvp.Value.Where(ranking => ranking.UserName == userName)) {
					badge |= kvp.Key;
				}
			}
			return badge;
		}
	}
}