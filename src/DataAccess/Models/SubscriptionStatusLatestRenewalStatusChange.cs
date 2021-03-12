using System;

namespace api.DataAccess.Models {
	public class SubscriptionStatusLatestRenewalStatusChange {
		public DateTime DateCreated { get; set; }
		public bool AutoRenewEnabled { get; set; }
		public string ProviderPriceId { get; set; }
		public string PriceLevelName { get; set; }
		public int? PriceAmount { get; set; }
	}
}