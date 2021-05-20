namespace api.DataAccess.Models {
	public class AuthorEarningsReportLineItem {
		public long AuthorId { get; set; }
		public string AuthorName { get; set; }
		public string AuthorSlug { get; set; }
		public long? UserAccountId { get; set; }
		public string UserAccountName { get; set; }
		public int MinutesRead { get; set; }
		public int AmountEarned { get; set; }
		public int AmountPaid { get; set; }
	}
}