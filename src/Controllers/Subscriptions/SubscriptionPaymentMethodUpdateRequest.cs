namespace api.Controllers.Subscriptions {
	public class SubscriptionPaymentMethodUpdateRequest {
		public string Id { get; set; }
		public int ExpirationMonth { get; set; }
		public int ExpirationYear { get; set; }
	}
}