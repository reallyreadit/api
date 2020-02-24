using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class TwitterWebForm {
		[BindProperty(Name = "oauth_token")]
		public string OAuthToken { get; set; }
		[BindProperty(Name = "oauth_verifier")]
		public string OAuthVerifier { get; set; }
	}
}