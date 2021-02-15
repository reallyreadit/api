namespace api.DataAccess.Models {
	public class SubscriptionPeriodAuthorDistribution {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderPeriodId { get; set; }
		public long AuthorId { get; set; }
		public int MinutesRead { get; set; }
		public int Amount { get; set; }
	}
}