using System;
using System.Collections.Generic;
using api.DataAccess.Models;

namespace api.DataAccess.Stats {
	public class Leaderboards {
		public IEnumerable<LeaderboardRanking> LongestRead { get; set; }
		public IEnumerable<LeaderboardRanking> ReadCount { get; set; }
		public IEnumerable<LeaderboardRanking> Scout { get; set; }
		public IEnumerable<LeaderboardRanking> Scribe { get; set; }
		public IEnumerable<LeaderboardRanking> Streak { get; set; }
		public IEnumerable<LeaderboardRanking> WeeklyReadCount { get; set; }
	}
}