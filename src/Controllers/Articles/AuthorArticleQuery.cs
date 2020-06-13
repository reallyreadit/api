namespace api.Controllers.Articles {
	public class AuthorArticleQuery : ArticleQuery {
		public int PageSize { get; set; }
		public string Slug { get; set; }
	}
}