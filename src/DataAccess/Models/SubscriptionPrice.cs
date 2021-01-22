using System;

namespace api.DataAccess.Models {
	public class SubscriptionPrice {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderPriceId { get; set; }
		public DateTime DateCreated { get; set; }
		public int? LevelId { get; set; }
		public int CustomAmount { get; set; }
	}
}