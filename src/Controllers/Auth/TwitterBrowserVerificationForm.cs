using api.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class TwitterBrowserVerificationForm {
		public string Denied { get; set; }
		[BindProperty(Name = "oauth_token")]
		public string OAuthToken { get; set; }
		[BindProperty(Name = "oauth_verifier")]
		public string OAuthVerifier { get; set; }
		[BindProperty(Name = "readup_integrations")]
		public AuthServiceIntegration ReadupIntegrations { get; set; }
		[BindProperty(Name = "readup_redirect_path")]
		public string ReadupRedirectPath { get; set; }
	}
}