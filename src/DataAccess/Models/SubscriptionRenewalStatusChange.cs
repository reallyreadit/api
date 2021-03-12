using System;

namespace api.DataAccess.Models {
	public class SubscriptionRenewalStatusChange {
		public long Id { get; set; }
		public SubscriptionProvider Provider { get; set; }
		public string ProviderSubscriptionId { get; set; }
		public DateTime DateCreated { get; set; }
		public bool AutoRenewEnabled { get; set; }
		public string ExpirationIntent { get; set; }
	}
}