using System;

namespace api.DataAccess.Models {
	public class SubscriptionPriceLevel {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderPriceId { get; set; }
		public DateTime DateCreated { get; set; }
		public int? LevelId { get; set; }
		public string Name { get; set; }
		public int Amount { get; set; }
	}
}