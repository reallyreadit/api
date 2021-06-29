using System;

namespace api.DataAccess.Models {
	public class MonthlyRecurringRevenueReportLineItem {
		public DateTime Period { get; set; }
		public int Amount { get; set; }
	}
}