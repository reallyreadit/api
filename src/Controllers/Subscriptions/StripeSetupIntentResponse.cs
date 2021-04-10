namespace api.Controllers.Subscriptions {
	public class StripeSetupIntentResponse {
		public StripeSetupIntentResponse(string clientSecret) {
			ClientSecret = clientSecret;
		}
		public string ClientSecret { get; }
	}
}