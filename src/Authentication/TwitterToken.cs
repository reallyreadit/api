namespace api.Authentication {
	public class TwitterToken {
		public TwitterToken(
			string oauthToken,
			string oauthTokenSecret
		) {
			OAuthToken = oauthToken;
			OAuthTokenSecret = oauthTokenSecret;
		}
		public string OAuthToken { get; }
		public string OAuthTokenSecret { get; }
	}
}