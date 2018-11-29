namespace api.DataAccess.Models {
	public class UserStats {
		public long ReadCount { get; set; }
		public long ReadCountRank { get; set; }
		public long? Streak { get; set; }
		public long? StreakRank { get; set; }
		public long UserCount { get; set; }
	}
}