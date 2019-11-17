namespace api.Controllers.Articles {
	public class ArticleQuery {
		public int PageNumber { get; set; }
		public int? MinLength { get; set; }
		public int? MaxLength { get; set; }
	}
}