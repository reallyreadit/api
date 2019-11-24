namespace api.Controllers.Social {
	public class CommentForm {
		public string Text { get; set; }
		public long ArticleId { get; set; }
		public string ParentCommentId { get; set; }
	}
}