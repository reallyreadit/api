using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.Controllers.Analytics {
	public class RevenueReportResponse {
		public RevenueReportResponse(
			IEnumerable<RevenueReportLineItem> lineItems
		) {
			LineItems = lineItems
				.Select(
					item => new RevenueReportLineItemClientModel(item)
				)
				.ToArray();
		}
		public RevenueReportLineItemClientModel[] LineItems { get; }
	}
}