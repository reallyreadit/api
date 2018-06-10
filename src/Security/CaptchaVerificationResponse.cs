using System;
using Newtonsoft.Json;

namespace api.Security {
	public class CaptchaVerificationResponse {
		public bool Success { get; set; }
		[JsonProperty(PropertyName = "challenge_ts")]
		public DateTime ChallengeTs { get; set; }
		public string Hostname { get; set; }
		[JsonProperty(PropertyName = "error-codes")]
		public string[] ErrorCodes { get; set; } = new string[0];
	}
}