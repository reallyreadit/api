using System;

namespace api.Controllers.Articles {
	public class PostCommentParams {
		public string Text { get; set; }
		public Guid ArticleId { get; set; }
	}
}