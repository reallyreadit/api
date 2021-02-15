using System;

namespace api.DataAccess.Models {
	public class SubscriptionPeriodDistribution {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderPeriodId { get; set; }
		public DateTime DateCreated { get; set; }
		public int PlatformAmount { get; set; }
		public int ProviderAmount { get; set; }
		public int UnknownAuthorMinutes { get; set; }
		public int UnknownAuthorAmount { get; set; }
	}
}