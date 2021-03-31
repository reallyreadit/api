using System;

namespace api.DataAccess.Models {
	public class SubscriptionPeriod {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderPeriodId { get; set; }
		public string ProviderSubscriptionId { get; set; }
		public string ProviderPriceId { get; set; }
		public string ProviderPaymentMethodId { get; set; }
		public DateTime BeginDate { get; set; }
		public DateTime EndDate { get; set; }
		public DateTime RenewalGracePeriodEndDate { get; set; }
		public DateTime DateCreated { get; set; }
		public SubscriptionPaymentStatus PaymentStatus { get; set; }
		public DateTime? DatePaid { get; set; }
		public DateTime? DateRefunded { get; set; }
		public string RefundReason { get; set; }
		public string NextProviderPeriodId { get; set; }
		public int? ProratedPriceAmount { get; set; }
	}
}