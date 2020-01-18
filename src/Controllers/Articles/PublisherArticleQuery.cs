namespace api.Controllers.Articles {
	public class PublisherArticleQuery : ArticleQuery {
		public int PageSize { get; set; }
		public string Slug { get; set; }
	}
}