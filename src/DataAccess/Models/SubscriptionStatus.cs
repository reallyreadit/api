using System;

namespace api.DataAccess.Models {
	public class SubscriptionStatus {
		public long UserAccountId { get; set; }
		public SubscriptionProvider Provider { get; set; }
		public string ProviderAccountId { get; set; }
		public string ProviderSubscriptionId { get; set; }
		public DateTime DateCreated { get; set; }
		public string LatestReceipt { get; set; }
		public SubscriptionStatusLatestPeriod LatestPeriod { get; set; }
		public SubscriptionState GetCurrentState(DateTime utcNow) {
			if (LatestPeriod.EndDate <= utcNow) {
				return SubscriptionState.Lapsed;
			}
			if (LatestPeriod.PaymentStatus == SubscriptionPaymentStatus.Succeeded) {
				return SubscriptionState.Active;
			}
			if (
				utcNow.Subtract(DateCreated) < TimeSpan.FromHours(23)
			) {
				return SubscriptionState.Incomplete;
			}
			return SubscriptionState.IncompleteExpired;
		}
	}
}