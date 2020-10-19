using api.Authentication;

namespace api.Controllers.Auth {
	public class BrowserPopupCookie {
		public long? IdentityId { get; set; }
		public AuthenticationError? Error { get; set; }
		public string Token { get; set; }
	}
}