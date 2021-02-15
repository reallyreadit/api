using api.Controllers.Shared;

namespace api.Controllers.Subscriptions {
	public class SubscriptionDistributionSummaryResponse {
		public SubscriptionDistributionSummaryResponse(
			SubscriptionStatusClientModel subscriptionStatus,
			SubscriptionDistributionReportClientModel currentPeriod,
			SubscriptionDistributionReportClientModel completedPeriods
		) {
			SubscriptionStatus = subscriptionStatus;
			CurrentPeriod = currentPeriod;
			CompletedPeriods = completedPeriods;
		}
		public object SubscriptionStatus { get; }
		public SubscriptionDistributionReportClientModel CurrentPeriod { get; }
		public SubscriptionDistributionReportClientModel CompletedPeriods { get; }
	}
}