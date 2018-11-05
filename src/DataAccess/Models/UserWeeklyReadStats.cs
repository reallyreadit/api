namespace api.DataAccess.Models {
	public class UserWeeklyReadStats {
		public long UserAccountId { get; set; }
		public long UserCount { get; set; }
		public long ReadCount { get; set; }
		public long ReadCountRank { get; set; }
		public long WordCount { get; set; }
		public long WordCountRank { get; set; }
	}
}