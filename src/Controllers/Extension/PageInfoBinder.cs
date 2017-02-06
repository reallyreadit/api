namespace api.Controllers.Extension {
	public class PageInfoBinder {
		public string Url { get; set; }
		public int Number { get; set; }
		public int WordCount { get; set; }
		public ArticleInfoBinder Article { get; set; }
	}
}