using api.DataAccess.Models;

namespace api.Controllers.Subscriptions {
	public class SubscriptionDistributionAuthorReportClientModel {
		public SubscriptionDistributionAuthorReportClientModel(
			SubscriptionDistributionAuthorReport report
		) {
			AuthorName = report.AuthorName;
			AuthorSlug = report.AuthorSlug;
			MinutesRead = report.MinutesRead;
			Amount = report.Amount;
		}
		public string AuthorName { get; }
		public string AuthorSlug { get; }
		public int MinutesRead { get;  }
		public int Amount { get; }
	}
}