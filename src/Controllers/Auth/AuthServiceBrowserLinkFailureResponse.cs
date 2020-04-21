using api.Authentication;

namespace api.Controllers.Auth {
	public class AuthServiceBrowserLinkFailureResponse : AuthServiceBrowserLinkResponse {
		public AuthServiceBrowserLinkFailureResponse(
			AuthenticationError error,
			string requestToken
		) : base(requestToken) {
			Error = error;
		}
		public AuthenticationError Error { get; }
	}	
}