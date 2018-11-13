namespace api.DataAccess.Models {
	public class UserReadStats {
		public long UserAccountId { get; set; }
		public long UserCount { get; set; }
		public long ReadCount { get; set; }
		public long ReadCountRank { get; set; }
	}
}