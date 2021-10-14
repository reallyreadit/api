using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public class WebAppUserProfileClientModel {
		public WebAppUserProfileClientModel(
			DisplayPreference displayPreference,
			SubscriptionStatusClientModel subscriptionStatus,
			UserAccount userAccount
		) {
			DisplayPreference = displayPreference;
			SubscriptionStatus = subscriptionStatus;
			UserAccount = userAccount;
		}
		public DisplayPreference DisplayPreference { get; }
		public object SubscriptionStatus { get; }
		public UserAccount UserAccount { get; }
	}
}