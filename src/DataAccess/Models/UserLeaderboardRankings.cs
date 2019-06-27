namespace api.DataAccess.Models {
	public class UserLeaderboardRankings {
		public Ranking LongestRead { get; set; }
		public Ranking ReadCount { get; set; }
		public Ranking ScoutCount { get; set; }
		public Ranking ScribeCount { get; set; }
		public StreakRanking Streak { get; set; }
		public Ranking WeeklyReadCount { get; set; }
	}
}