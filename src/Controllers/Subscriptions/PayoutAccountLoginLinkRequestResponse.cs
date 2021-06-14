namespace api.Controllers.Subscriptions {
	public class PayoutAccountLoginLinkRequestResponse {
		public PayoutAccountLoginLinkRequestResponse(string loginUrl) {
			LoginUrl = loginUrl;
		}
		public string LoginUrl { get; }
	}
}