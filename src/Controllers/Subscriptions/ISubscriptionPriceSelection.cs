namespace api.Controllers.Subscriptions {
	public interface ISubscriptionPriceSelection {
		string PriceLevelId { get; }
		int CustomPriceAmount { get; }
	}
}