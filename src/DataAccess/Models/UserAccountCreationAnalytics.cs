using api.Analytics;

namespace api.DataAccess.Models {
	public class UserAccountCreationAnalytics {
		public ClientAnalytics Client { get; set; }
		public int MarketingScreenVariant { get; set; }
		public string ReferrerUrl { get; set; }
		public string InitialPath { get; set; }
	}
}