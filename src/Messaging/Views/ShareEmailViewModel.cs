using System;
using api.Configuration;
using api.Controllers;
using api.DataAccess.Models;

namespace api.Messaging.Views {
	public class ShareEmailViewModel : EmailLayoutViewModel {
		public ShareEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			UserAccount sender,
			UserArticle article,
			string message
		) : base(title, webServerEndpoint) {
			Sender = sender.Name;
			ArticleUrl = webServerEndpoint.CreateUrl(RouteHelper.GetArticlePath(article.Slug));
			ArticleTitle = article.Title;
			Message = message;
		}
		public string Sender { get; }
		public string ArticleUrl { get; }
		public string ArticleTitle { get; }
		public string Message { get; }
	}
}