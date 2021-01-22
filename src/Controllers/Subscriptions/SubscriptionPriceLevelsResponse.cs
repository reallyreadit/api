using System.Collections.Generic;
using api.Controllers.Shared;

namespace api.Controllers.Subscriptions {
	public class SubscriptionPriceLevelsResponse {
		public SubscriptionPriceLevelsResponse(
			IEnumerable<SubscriptionPriceClientModel> prices
		) {
			Prices = prices;
		}
		public IEnumerable<SubscriptionPriceClientModel> Prices { get; }
	}
}