namespace api.Authentication {
	public interface ITwitterToken {
		string OAuthToken { get; }
		string OAuthTokenSecret { get; }
	}
}