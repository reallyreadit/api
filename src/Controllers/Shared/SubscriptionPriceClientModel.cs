namespace api.Controllers.Shared {
	public class SubscriptionPriceClientModel {
		public SubscriptionPriceClientModel(
			string id,
			string name,
			int amount
		) {
			Id = id;
			Name = name;
			Amount = amount;
		}
		public string Id { get; }
		public string Name { get; }
		public int Amount { get; }
	}
}