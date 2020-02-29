using api.Analytics;
using api.Authentication;
using api.Notifications;

namespace api.Controllers.Auth {
	public class TwitterCredentialLinkForm {
		public AuthServiceIntegration Integrations { get; set; }
		public string OAuthToken { get; set; }
		public string OAuthVerifier { get; set; }
	}
}