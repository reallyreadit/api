using api.Analytics;

namespace api.DataAccess.Models {
	public class UserAccountCreationAnalytics : SignUpAnalyticsForm {
		public UserAccountCreationAnalytics() { }
		public UserAccountCreationAnalytics(
			ClientAnalytics client,
			SignUpAnalyticsForm form
		) {
			Client = client;
			MarketingVariant = form.MarketingVariant;
			ReferrerUrl = form.ReferrerUrl;
			InitialPath = form.InitialPath;
			CurrentPath = form.CurrentPath;
			Action = form.Action;
		}
		public ClientAnalytics Client { get; set; }
	}
}