using api.DataAccess.Models;

namespace api.Controllers.Embed {
	public class ReadProgressResponse {
		public ReadProgressResponse(
			Article article
		) {
			Article = article;
		}
		public Article Article { get; }
	}
}