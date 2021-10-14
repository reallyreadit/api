using api.Controllers.Shared;

namespace api.Controllers.Subscriptions {
	public class FreeTrialPromoTweetIntentResponse {
		public FreeTrialPromoTweetIntentResponse(
			SubscriptionStatusClientModel subscriptionStatus
		) {
			SubscriptionStatus = subscriptionStatus;
		}
		public object SubscriptionStatus { get; }
	}
}