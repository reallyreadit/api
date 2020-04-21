using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class TwitterAuthenticationCallbackRequest : TwitterCallbackRequest {
		[BindProperty(Name = "readup_redirect_path")]
		public string ReadupRedirectPath { get; set; }
	}
}