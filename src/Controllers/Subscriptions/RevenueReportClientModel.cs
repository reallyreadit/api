using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.Controllers.Subscriptions {
	public class RevenueReportClientModel {
		public RevenueReportClientModel(
			SubscriptionAllocationCalculation calculation,
			AuthorEarningsTotalsReport earningsReport,
			PayoutTotalsReport payoutTotalsReport
		) {
			TotalRevenue = calculation.AuthorAmount + calculation.PlatformAmount + calculation.ProviderAmount;
			AuthorAllocation = calculation.AuthorAmount;
			AuthorEarnings = earningsReport.TotalAuthorEarnings;
			TotalPayouts = payoutTotalsReport.TotalAuthorPayouts + payoutTotalsReport.TotalDonationPayouts;
		}
		public int TotalRevenue { get; }
		public int AuthorAllocation { get; }
		public int AuthorEarnings { get; }
		public int TotalPayouts { get; }
	}
}