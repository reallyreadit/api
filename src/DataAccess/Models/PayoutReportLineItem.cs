namespace api.DataAccess.Models {
	public class PayoutReportLineItem {
		public string AuthorName { get; set; }
		public int TotalEarnings { get; set; }
		public int TotalPayouts { get; set; }
		public int TotalDonations { get; set; }
		public int CurrentBalance { get; set; }
	}
}