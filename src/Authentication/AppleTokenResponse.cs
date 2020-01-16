namespace api.Authentication {
	public class AppleTokenResponse {
		public string AccessToken { get; set; }
		public long ExpiresIn { get; set; }
		public string IdToken { get; set; }
		public string RefreshToken { get; set; }
		public string TokenType { get; set; }
	}
}