using System;

namespace api.DataAccess.Models {
	public class Rating {
		public long Id { get; set; }
		public DateTime Timestamp { get; set; }
		public int Score { get; set; }
		public long ArticleId { get; set; }
		public long UserAccountId { get; set; }
	}
}