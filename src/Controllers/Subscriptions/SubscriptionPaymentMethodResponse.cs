using api.Controllers.Shared;

namespace api.Controllers.Subscriptions {
	public class SubscriptionPaymentMethodResponse {
		public SubscriptionPaymentMethodResponse(
			SubscriptionPaymentMethodClientModel paymentMethod
		) {
			PaymentMethod = paymentMethod;
		}
		public SubscriptionPaymentMethodClientModel PaymentMethod { get;}
	}
}