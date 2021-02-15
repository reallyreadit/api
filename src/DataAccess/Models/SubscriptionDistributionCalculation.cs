namespace api.DataAccess.Models {
	public class SubscriptionDistributionCalculation {
		public int PlatformAmount { get; set; }
		public int ProviderAmount { get; set; }
		public int UnknownAuthorMinutesRead { get; set; }
		public int UnknownAuthorAmount { get; set; }
		public SubscriptionDistributionAuthorCalculation[] AuthorDistributions { get; set; }
	}
}