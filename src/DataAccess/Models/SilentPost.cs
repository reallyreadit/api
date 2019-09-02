using System;

namespace api.DataAccess.Models {
	public class SilentPost {
		public long Id { get; set; }
		public long ArticleId { get; set; }
		public long UserAccountId { get; set; }
		public DateTime DateCreated { get; set; }
	}
}