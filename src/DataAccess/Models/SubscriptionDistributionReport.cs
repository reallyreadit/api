namespace api.DataAccess.Models {
	public class SubscriptionDistributionReport {
		public int SubscriptionAmount { get; set; }
		public int PlatformAmount { get; set; }
		public int AppleAmount { get; set; }
		public int StripeAmount { get; set; }
		public int UnknownAuthorMinutesRead { get; set; }
		public int UnknownAuthorAmount { get; set; }
		public SubscriptionDistributionAuthorReport[] AuthorDistributions { get; set; }
	}
}