namespace api.Controllers.Articles {
	public class RateArticleForm {
		public long ArticleId { get; set; }
		public long UserAccountId { get; set; }
		public int Score { get; set; }
	}
}