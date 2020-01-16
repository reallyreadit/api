using api.Analytics;

namespace api.DataAccess.Models {
	public class UserAccountCreationAnalytics : SignUpAnalyticsForm {
		public UserAccountCreationAnalytics(
			ClientAnalytics client,
			int marketingVariant,
			string referrerUrl,
			string initialPath,
			string currentPath,
			string action
		) {
			Client = client;
			MarketingVariant = marketingVariant;
			ReferrerUrl = referrerUrl;
			InitialPath = initialPath;
			CurrentPath = currentPath;
			Action = action;
		}
		public ClientAnalytics Client { get; }
	}
}