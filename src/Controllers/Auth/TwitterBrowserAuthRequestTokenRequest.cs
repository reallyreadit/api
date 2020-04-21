using api.Analytics;
using api.Authentication;

namespace api.Controllers.Auth {
	public class TwitterBrowserAuthRequestTokenRequest {
		public string RedirectPath { get; set; }
		public SignUpAnalyticsForm SignUpAnalytics { get; set; }
	}
}