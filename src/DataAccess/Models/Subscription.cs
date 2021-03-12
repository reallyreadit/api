using System;

namespace api.DataAccess.Models {
	public class Subscription {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderSubscriptionId { get; set; }
		public string ProviderAccountId { get; set; }
		public DateTime DateCreated { get; set; }
		public string LatestReceipt { get; set; }
	}
}