namespace api.DataAccess.Models {
	public class UserArticle : Article {
		public string Source { get; set; }
		public string Url { get; set; }
		public int CommentCount { get; set; }
		public int PageCount { get; set; }
		public double PercentComplete { get; set; }
	}
}