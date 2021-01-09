using api.Authentication;
using api.Controllers.Shared;

namespace api.Controllers.Auth {
	public class BrowserPopupResponseResponse {
		public BrowserPopupResponseResponse(AuthServiceAccountAssociation association) {
			Association = association;
		}
		public BrowserPopupResponseResponse(string authServiceToken) {
			AuthServiceToken = authServiceToken;
		}
		public BrowserPopupResponseResponse(AuthenticationError error) {
			Error = error;
		}
		public BrowserPopupResponseResponse(WebAppUserProfileViewModel userProfile) {
			UserProfile = userProfile;
		}
		public AuthServiceAccountAssociation Association { get; }
		public string AuthServiceToken { get; }
		public AuthenticationError? Error { get; }
		public WebAppUserProfileViewModel UserProfile { get; }
	}
}