using api.Analytics;

namespace api.Controllers.Auth {
	public class AppleWebRedirectState : SignUpAnalyticsForm {
		public string Client { get; set; }
	}
}