using api.DataAccess.Models;

namespace api.Controllers.Subscriptions {
	public class AuthorEarningsReportLineItemClientModel {
		public AuthorEarningsReportLineItemClientModel(
			AuthorEarningsReportLineItem lineItem
		) {
			AuthorName = lineItem.AuthorName;
			AuthorSlug = lineItem.AuthorSlug;
			UserAccountName = lineItem.UserAccountName;
			MinutesRead = lineItem.MinutesRead;
			AmountEarned = lineItem.AmountEarned;
		}
		public string AuthorName { get; }
		public string AuthorSlug { get; }
		public string UserAccountName { get; }
		public int MinutesRead { get; }
		public int AmountEarned { get; }
	}
}