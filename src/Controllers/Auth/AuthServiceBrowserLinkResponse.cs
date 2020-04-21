namespace api.Controllers.Auth {
	public abstract class AuthServiceBrowserLinkResponse {
		public AuthServiceBrowserLinkResponse(string requestToken) {
			RequestToken = requestToken;
		}
		public string RequestToken { get; }
	}
}