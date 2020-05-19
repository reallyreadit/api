using System;

namespace api.DataAccess.Models {
	public class ArticleImage {
		public long ArticleId { get; set; }
		public DateTime DateCreated { get; set; }
		public long CreatorUserId { get; set; }
		public string Url { get; set; }
	}
}