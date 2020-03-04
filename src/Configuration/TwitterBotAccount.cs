using System;
using api.Authentication;

namespace api.Configuration {
	public class TwitterBotAccount : ITwitterToken {
		public string Handle { get; set; }
		public string OAuthToken { get; set; }
		public string OAuthTokenSecret { get; set; }
		public bool IsEnabled => (
			!String.IsNullOrWhiteSpace(OAuthToken) &&
			!String.IsNullOrWhiteSpace(OAuthTokenSecret)
		);
	}
}