using api.Controllers.Shared;

namespace api.Controllers.Subscriptions {
	public class SubscriptionStatusResponse {
		public SubscriptionStatusResponse(
			SubscriptionStatusClientModel status
		) {
			Status = status;
		}
		public object Status { get; }
	}
}