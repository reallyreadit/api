using api.Analytics;
using api.Authentication;

namespace api.Controllers.Auth {
	public class TwitterBrowserRequestForm {
		public AuthServiceIntegration Integrations { get; set; }
		public string RedirectPath { get; set; }
		public SignUpAnalyticsForm SignUpAnalytics { get; set; }
	}
}