using api.Analytics;
using api.Authentication;
using api.Notifications;

namespace api.Controllers.Auth {
	public class TwitterCredentialAuthForm {
		public string OAuthToken { get; set; }
		public string OAuthVerifier { get; set; }
		public SignUpAnalyticsForm Analytics { get; set; }
		public PushDeviceForm PushDevice { get; set; }
	}
}