using System;

namespace api.DataAccess.Models {
	public class Comment {
		public Guid Id { get; set; }
		public DateTime DateCreated { get; set; }
		public string Text { get; set; }
		public Guid ArticleId { get; set; }
		public string ArticleTitle { get; set; }
		public string ArticleSlug { get; set; }
		public Guid UserAccountId { get; set; }
		public string UserAccount { get; set; }
		public Guid? ParentCommentId { get; set; }
	}
}