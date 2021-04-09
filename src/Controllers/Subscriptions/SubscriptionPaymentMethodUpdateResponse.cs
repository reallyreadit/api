using api.Controllers.Shared;

namespace api.Controllers.Subscriptions {
	public class SubscriptionPaymentMethodUpdateResponse {
		public SubscriptionPaymentMethodUpdateResponse(
			SubscriptionPaymentMethodClientModel paymentMethod
		) {
			PaymentMethod = paymentMethod;
		}
		public SubscriptionPaymentMethodClientModel PaymentMethod { get;}
	}
}