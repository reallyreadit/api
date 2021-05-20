using System;

namespace api.DataAccess.Models {
	public class RevenueReportLineItem {
		public DateTime Period { get; set; }
		public SubscriptionProvider? Provider { get; set; }
		public string PriceName { get; set; }
		public int PriceAmount { get; set; }
		public int QuantityPurchased { get; set; }
		public int QuantityRefunded { get; set; }
	}
}