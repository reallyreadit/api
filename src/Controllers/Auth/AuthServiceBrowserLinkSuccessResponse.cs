using api.Authentication;

namespace api.Controllers.Auth {
	public class AuthServiceBrowserLinkSuccessResponse : AuthServiceBrowserLinkResponse {
		public AuthServiceBrowserLinkSuccessResponse(
			AuthServiceAccountAssociation association,
			string requestToken
		) : base(requestToken) {
			Association = association;
		}
		public AuthServiceAccountAssociation Association { get; }
	}
}