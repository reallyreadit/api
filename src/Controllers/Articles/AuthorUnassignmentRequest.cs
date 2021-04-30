namespace api.Controllers.Articles {
	public class AuthorUnassignmentRequest {
		public string ArticleSlug { get; set; }
		public string AuthorSlug { get; set; }
	}
}