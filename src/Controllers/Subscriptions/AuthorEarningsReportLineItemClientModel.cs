using api.DataAccess.Models;

namespace api.Controllers.Subscriptions {
	public class AuthorEarningsReportLineItemClientModel {
		public AuthorEarningsReportLineItemClientModel(
			AuthorEarningsReportLineItem lineItem,
			Article topArticle
		) {
			AuthorName = lineItem.AuthorName;
			AuthorSlug = lineItem.AuthorSlug;
			UserAccountName = lineItem.UserAccountName;
			DonationRecipientName = lineItem.DonationRecipientName;
			MinutesRead = lineItem.MinutesRead;
			AmountEarned = lineItem.AmountEarned;
			if (lineItem.DonationRecipientId.HasValue) {
				Status = AuthorEarningsReportLineItemStatus.DonationPaidOut;
			} else if (lineItem.AmountPaid > 0) {
				Status = AuthorEarningsReportLineItemStatus.AuthorPaidOut;
			} else if (lineItem.AuthorContactStatus == AuthorContactStatus.Attempted) {
				Status = AuthorEarningsReportLineItemStatus.Contacted;
			} else if (lineItem.AmountEarned >= 1000) {
				Status = AuthorEarningsReportLineItemStatus.NotYetContacted;
			} else {
				Status = AuthorEarningsReportLineItemStatus.ApproachingMinimum;
			}
			TopArticle = topArticle;
		}
		public string AuthorName { get; }
		public string AuthorSlug { get; }
		public string UserAccountName { get; }
		public string DonationRecipientName { get; }
		public int MinutesRead { get; }
		public int AmountEarned { get; }
		public AuthorEarningsReportLineItemStatus Status { get; }
		public Article TopArticle { get; }
	}
}