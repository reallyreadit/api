namespace api.Controllers.Subscriptions {
	public class StripeSubscriptionCreationRequest : ISubscriptionPriceSelection {
		public string PaymentMethodId { get; set; }
		public string PriceLevelId { get; set; }
		public int CustomPriceAmount { get; set; }
	}
}