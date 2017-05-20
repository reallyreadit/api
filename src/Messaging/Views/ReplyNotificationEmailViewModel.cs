using System;
using api.Configuration;

namespace api.Messaging.Views {
	public class ReplyNotificationEmailViewModel : EmailLayoutViewModel {
		public ReplyNotificationEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			string unsubscribeToken,
			string respondent,
			string articleTitle,
			string replyToken,
			HttpEndpointOptions apiServerEndpoint
		) : base(title, webServerEndpoint) {
			UnsubscribeToken = unsubscribeToken;
			Respondent = respondent;
			ArticleTitle = articleTitle;
			ReplyToken = replyToken;
			ApiServerEndpoint = apiServerEndpoint;
		}
		public string UnsubscribeToken { get; }
		public string Respondent { get; }
		public string ArticleTitle { get; }
		public string ReplyToken { get; }
		public HttpEndpointOptions ApiServerEndpoint { get; }
	}
}