using Microsoft.AspNetCore.Http;

namespace api.Configuration {
	public class AuthenticationOptions {
		public string ApiKey { get; set; }
		public AppleAuthOptions AppleAuth { get; set; }
		public string Scheme { get; set; }
		public string CookieName { get; set; }
		public string CookieDomain { get; set; }
		public CookieSecurePolicy CookieSecure { get; set; }
		public TwitterAuthOptions TwitterAuth { get; set; }
	}
}