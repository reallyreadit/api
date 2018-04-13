namespace api.Controllers.Articles {
	public class PostCommentBinder {
		public string Text { get; set; }
		public long ArticleId { get; set; }
		public long? ParentCommentId { get; set; }
	}
}