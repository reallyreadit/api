using api.DataAccess.Models;

namespace api.Controllers.Subscriptions {
	public class RevenueReportResponse {
		public RevenueReportResponse(
			RevenueReportClientModel report
		) {
			Report = report;
		}
		public RevenueReportClientModel Report { get; }
	}
}