using System;

namespace api.Controllers.Articles {
	public class PostCommentBinder {
		public string Text { get; set; }
		public Guid ArticleId { get; set; }
		public Guid? ParentCommentId { get; set; }
	}
}