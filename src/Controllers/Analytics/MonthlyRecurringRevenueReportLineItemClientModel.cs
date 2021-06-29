using System;
using api.DataAccess.Models;

namespace api.Controllers.Analytics {
	public class MonthlyRecurringRevenueReportLineItemClientModel {
		public MonthlyRecurringRevenueReportLineItemClientModel(MonthlyRecurringRevenueReportLineItem lineItem) {
			Period = lineItem.Period;
			Amount = lineItem.Amount;
		}
		public DateTime Period { get; }
		public int Amount { get; }
	}
}