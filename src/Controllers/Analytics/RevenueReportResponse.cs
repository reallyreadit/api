using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.Controllers.Analytics {
	public class RevenueReportResponse {
		public RevenueReportResponse(
			IEnumerable<RevenueReportLineItem> lineItems,
			IEnumerable<MonthlyRecurringRevenueReportLineItem> monthlyRecurringRevenueLineItems
		) {
			LineItems = lineItems
				.Select(
					item => new RevenueReportLineItemClientModel(item)
				)
				.ToArray();
			MonthlyRecurringRevenueLineItems = monthlyRecurringRevenueLineItems
				.Select(
					item => new MonthlyRecurringRevenueReportLineItemClientModel(item)
				)
				.ToArray();
		}
		public RevenueReportLineItemClientModel[] LineItems { get; }
		public MonthlyRecurringRevenueReportLineItemClientModel[] MonthlyRecurringRevenueLineItems { get; }
	}
}