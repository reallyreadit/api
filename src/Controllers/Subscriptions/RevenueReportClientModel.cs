using api.DataAccess.Models;

namespace api.Controllers.Subscriptions {
	public class RevenueReportClientModel {
		public RevenueReportClientModel(
			SubscriptionAllocationCalculation calculation
		) {
			TotalRevenue = calculation.AuthorAmount + calculation.PlatformAmount + calculation.ProviderAmount;
			AuthorAllocation = calculation.AuthorAmount;
		}
		public int TotalRevenue { get; }
		public int AuthorAllocation { get; }
	}
}