using System;
using api.DataAccess.Models;
using api.Messaging.Views.Shared;

namespace api.Messaging.Views {
	public class AotdEmailViewModel {
		public AotdEmailViewModel(
			Article article,
			Uri readArticleUrl,
			Uri viewCommentsUrl,
			Uri learnMoreUrl
		) {
			Article = new ArticleViewModel(article, readArticleUrl, viewCommentsUrl);
			LearnMoreUrl = learnMoreUrl.ToString();
		}
		public ArticleViewModel Article { get; }
		public string LearnMoreUrl { get; }
	}
}