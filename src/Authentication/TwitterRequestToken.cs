namespace api.Authentication {
	public class TwitterRequestToken : TwitterToken {
		public TwitterRequestToken(
			string oauthToken,
			string oauthTokenSecret,
			bool oauthCallbackConfirmed
		) : base (
			oauthToken,
			oauthTokenSecret
		) {
			OAuthCallbackConfirmed = oauthCallbackConfirmed;
		}
		public bool OAuthCallbackConfirmed { get; }
	}
}