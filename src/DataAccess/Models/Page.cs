namespace api.DataAccess.Models {
	public class Page {
		public long Id { get; set; }
		public long ArticleId { get; set; }
		public int Number { get; set; }
		public int WordCount { get; set; }
		public int ReadableWordCount { get; set; }
		public string Url { get; set; }
	}
}