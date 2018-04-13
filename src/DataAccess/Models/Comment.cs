using System;

namespace api.DataAccess.Models {
	public class Comment {
		public long Id { get; set; }
		public DateTime DateCreated { get; set; }
		public string Text { get; set; }
		public long ArticleId { get; set; }
		public string ArticleTitle { get; set; }
		public string ArticleSlug { get; set; }
		public long UserAccountId { get; set; }
		public string UserAccount { get; set; }
		public long? ParentCommentId { get; set; }
		public DateTime? DateRead { get; set; }
	}
}