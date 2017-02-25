namespace api.DataAccess.Models {
	public class UserArticle : Article {
		public string Source { get; set; }
		public string Url { get; set; }
		public string[] Authors { get; set; }
		public string[] Tags { get; set; }
		public int WordCount { get; set; }
		public int PageCount { get; set; }
		public double PercentComplete { get; set; }
		public int CommentCount { get; set; }
	}
}