using Microsoft.AspNetCore.Http;

namespace api.Configuration {
	public class AuthenticationOptions {
		public string ApiKey { get; set; }
		public AppleAuthOptions AppleAuth { get; set; }
		public string Scheme { get; set; }
		public string CookieName { get; set; }
		public TwitterAuthOptions TwitterAuth { get; set; }
	}
}