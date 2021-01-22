namespace api.Configuration {
	public class SubscriptionsOptions {
		public string AppleAppSecret { get; set; }
		public string AppStoreSandboxUrl { get; set; }
		public string AppStoreProductionUrl { get; set; }
		public string StripeApiSecretKey { get; set; }
		public string StripeSubscriptionProductId { get; set; }
	}
}