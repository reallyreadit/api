namespace api.Controllers.Social {
	public class PostForm {
		public long ArticleId { get; set; }
		public int? RatingScore { get; set; }
		public string CommentText { get; set; }
		public bool Tweet { get; set; }
	}
}