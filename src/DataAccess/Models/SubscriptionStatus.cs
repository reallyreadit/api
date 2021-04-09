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
		public SubscriptionStatusLatestRenewalStatusChange LatestRenewalStatusChange { get; set; }
		public SubscriptionState GetCurrentState(DateTime utcNow) {
			if (LatestPeriod.PaymentStatus == SubscriptionPaymentStatus.Succeeded) {
				if (LatestPeriod.RenewalGracePeriodEndDate > utcNow && !LatestPeriod.DateRefunded.HasValue) {
					return SubscriptionState.Active;
				}
				return SubscriptionState.Lapsed;
			}
			if (LatestPeriod.DateCreated == DateCreated) {
				if (
					utcNow.Subtract(LatestPeriod.DateCreated) < TimeSpan.FromHours(23)
				) {
					return SubscriptionState.Incomplete;
				}
				return SubscriptionState.IncompleteExpired;
			}
			if (
				LatestPeriod.PaymentStatus == SubscriptionPaymentStatus.RequiresConfirmation &&
				utcNow.Subtract(LatestPeriod.DateCreated) < TimeSpan.FromHours(23)
			) {
				return SubscriptionState.RenewalRequiresConfirmation;
			}
			return SubscriptionState.RenewalFailed;
		}
		public bool IsAutoRenewEnabled() => LatestRenewalStatusChange == null || LatestRenewalStatusChange.AutoRenewEnabled;
	}
}