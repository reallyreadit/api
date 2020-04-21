using System;
using api.Authentication;

namespace api.Configuration {
	public class TwitterAccountOptions : ITwitterToken {
		public string Handle { get; set; }
		public string OAuthToken { get; set; }
		public string OAuthTokenSecret { get; set; }
	}
}