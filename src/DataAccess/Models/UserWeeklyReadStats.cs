namespace api.DataAccess.Models {
	public class UserWeeklyReadStats {
		public int UserAccountId { get; set; }
		public int UserCount { get; set; }
		public int ReadCount { get; set; }
		public int ReadCountRank { get; set; }
		public int WordCount { get; set; }
		public int WordCountRank { get; set; }
	}
}