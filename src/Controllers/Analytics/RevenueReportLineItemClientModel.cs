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