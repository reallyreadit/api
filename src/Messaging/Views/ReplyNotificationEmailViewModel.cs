using System;
using api.Configuration;

namespace api.Messaging.Views {
	public class ReplyNotificationEmailViewModel : EmailLayoutViewModel {
		public ReplyNotificationEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			string subscriptionsToken,
			string respondent,
			string articleTitle,
			string replyToken,
			HttpEndpointOptions apiServerEndpoint
		) : base(title, webServerEndpoint) {
			SubscriptionsToken = subscriptionsToken;
			Respondent = respondent;
			ArticleTitle = articleTitle;
			ReplyToken = replyToken;
			ApiServerEndpoint = apiServerEndpoint;
		}
		public string SubscriptionsToken { get; }
		public string Respondent { get; }
		public string ArticleTitle { get; }
		public string ReplyToken { get; }
		public HttpEndpointOptions ApiServerEndpoint { get; }
	}
}