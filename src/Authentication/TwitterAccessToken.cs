namespace api.Authentication {
	public class TwitterAccessToken : TwitterToken {
		public TwitterAccessToken(
			string oauthToken,
			string oauthTokenSecret,
			string screenName,
			string userId
		) : base (
			oauthToken: oauthToken,
			oauthTokenSecret: oauthTokenSecret
		) {
			ScreenName = screenName;
			UserId = userId;
		}
		public string ScreenName { get; }
		public string UserId { get; }
	}
}