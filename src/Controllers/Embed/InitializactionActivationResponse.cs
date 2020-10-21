using api.DataAccess.Models;

namespace api.Controllers.Embed {
	public class InitializationActivationResponse {
		public InitializationActivationResponse(
			Article article,
			UserAccount user,
			UserArticle userArticle
		) {
			Action = InitializationAction.Activate;
			Article = article;
			User = user;
			UserArticle = userArticle;
		}
		public InitializationAction Action { get; }
		public Article Article { get; }
		public UserAccount User { get; }
		public UserArticle UserArticle { get; }
	}
}