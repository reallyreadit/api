// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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