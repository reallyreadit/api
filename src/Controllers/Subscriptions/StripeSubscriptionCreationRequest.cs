namespace api.Controllers.Subscriptions {
	public class StripeSubscriptionCreationRequest {
		public string PaymentMethodId { get; set; }
		public string PriceLevelId { get; set; }
		public int CustomPriceAmount { get; set; }
	}
}