using System;

namespace api.DataAccess.Models {
	public class SubscriptionAccount {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderAccountId { get; set; }
		public long? UserAccountId { get; set; }
		public DateTime DateCreated { get; set; }
	}
}