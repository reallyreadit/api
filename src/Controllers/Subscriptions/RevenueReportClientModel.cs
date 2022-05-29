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

namespace api.Controllers.Subscriptions {
	public class RevenueReportClientModel {
		public RevenueReportClientModel(
			SubscriptionAllocationCalculation calculation,
			IEnumerable<AuthorEarningsReportLineItem> earningsReport,
			PayoutTotalsReport payoutTotalsReport
		) {
			TotalRevenue = calculation.AuthorAmount + calculation.PlatformAmount + calculation.ProviderAmount;
			AuthorAllocation = calculation.AuthorAmount;
			AuthorEarnings = earningsReport.Sum(
				item => item.AmountEarned
			);
			TotalPayouts = payoutTotalsReport.TotalAuthorPayouts + payoutTotalsReport.TotalDonationPayouts;
		}
		public int TotalRevenue { get; }
		public int AuthorAllocation { get; }
		public int AuthorEarnings { get; }
		public int TotalPayouts { get; }
	}
}