namespace api.DataAccess.Models {
	public class SubscriptionDistributionAuthorReport {
		public long AuthorId { get; set; }
		public string AuthorName { get; set; }
		public string AuthorSlug { get; set; }
		public int MinutesRead { get; set; }
		public int Amount { get; set; }
	}
}