using api.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;

namespace api.Controllers.Subscriptions {
	public class PayoutReportResponse {
		public PayoutReportResponse(IEnumerable<PayoutReportLineItem> lineItems) {
			LineItems = lineItems.ToArray();
		}
		public PayoutReportLineItem[] LineItems { get; }
	}
}