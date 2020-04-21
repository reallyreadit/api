using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class TwitterCallbackRequest {
		public string Denied { get; set; }
		[BindProperty(Name = "oauth_token")]
		public string OAuthToken { get; set; }
		[BindProperty(Name = "oauth_verifier")]
		public string OAuthVerifier { get; set; }
	}
}