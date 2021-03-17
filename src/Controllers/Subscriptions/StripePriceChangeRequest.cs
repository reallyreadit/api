namespace api.Controllers.Subscriptions {
	public class StripePriceChangeRequest : ISubscriptionPriceSelection {
		public string PriceLevelId { get; set; }
		public int CustomPriceAmount { get; set; }
	}
}