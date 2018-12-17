namespace api.Encryption {
	public static class UrlSafeBase64 {
		public static string Encode(string base64String) {
			if (base64String == null) {
				return base64String;
			}
			return base64String
				.Replace('+', '.')
				.Replace('/', '_')
				.Replace('=', '-');
		}
		public static string Decode(string urlSafeBase64String) {
			if (urlSafeBase64String == null) {
				return urlSafeBase64String;
			}
			return urlSafeBase64String
				.Replace('.', '+')
				.Replace('_', '/')
				.Replace('-', '=');
		}
	}
}