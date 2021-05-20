using System.Collections.Generic;
using System.Linq;

namespace api.Controllers.Subscriptions {
	public class AuthorsEarningsReportResponse {
		public AuthorsEarningsReportResponse(
			IEnumerable<AuthorEarningsReportLineItemClientModel> lineItems
		) {
			LineItems = lineItems.ToArray();
		}
		public AuthorEarningsReportLineItemClientModel[] LineItems { get; }
	}
}