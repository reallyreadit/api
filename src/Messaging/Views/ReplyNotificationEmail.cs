using api.Configuration;

namespace api.Messaging.Views {
	public class ReplyNotificationEmail: EmailLayoutViewModel {
		public ReplyNotificationEmail(
			HttpEndpointOptions apiServerEndpoint,
			HttpEndpointOptions webServerEndpoint,
			string openToken,
			string viewCommentToken,
			string subscriptionsToken,
			string title,
			string respondent,
			string commentText
		) : base(
			title: title,
			webServerEndpoint: webServerEndpoint
		) {
			ApiServerEndpoint = apiServerEndpoint;
			OpenToken = openToken;
			ViewCommentToken = viewCommentToken;
			SubscriptionsToken = subscriptionsToken;
			Respondent = respondent;
			CommentText = commentText;
		}
		public HttpEndpointOptions ApiServerEndpoint { get; }
		public string OpenToken { get; }
		public string ViewCommentToken { get; }
		public string SubscriptionsToken { get; }
		public string Respondent { get; }
		public string CommentText { get; }
	}
}