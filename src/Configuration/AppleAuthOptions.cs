namespace api.Configuration {
	public class AppleAuthOptions {
		public string AppleJwkUrl { get; set; }
		public string ClientSecretAudience { get; set; }
		public string ClientSecretSigningKeyId { get; set; }
		public string ClientSecretSigningKeyPath { get; set; }
		public string DeveloperAppId { get; set; }
		public string DeveloperTeamId { get; set; }
		public string DeveloperWebServiceId { get; set; }
		public string IdTokenIssuer { get; set; }
		public string IdTokenValidationUrl { get; set; }
		public string WebAuthPopupRedirectUrl { get; set; }
		public string WebAuthRedirectUrl { get; set; }
		public string WebAuthUrl { get; set; }
	}
}