// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System;
using api.Controllers.Shared;
using api.DataAccess.Models;

namespace api.Controllers.Analytics {
	public class RevenueReportLineItemClientModel {
		public RevenueReportLineItemClientModel(
			RevenueReportLineItem item
		) {
			Period = item.Period;
			if (item.Provider.HasValue) {
				Provider = SubscriptionProviderClientValueExtensions.FromSubscriptionProvider(item.Provider.Value);
			}
			PriceName = item.PriceName;
			PriceAmount = item.PriceAmount;
			QuantityPurchased = item.QuantityPurchased;
			QuantityRefunded = item.QuantityRefunded;
		}
		public DateTime Period { get; }
		public SubscriptionProviderClientValue? Provider { get; }
		public string PriceName { get; }
		public int PriceAmount { get; }
		public int QuantityPurchased { get; }
		public int QuantityRefunded { get; }
	}
}