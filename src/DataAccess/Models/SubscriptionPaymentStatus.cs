using api.Subscriptions;

namespace api.DataAccess.Models {
	public enum SubscriptionPaymentStatus {
		Succeeded,
		RequiresConfirmation,
		Failed
	}
	public static class SubscriptionPaymentStatusExtensions {
		public static SubscriptionPaymentStatus FromStripePaymentIntentStatusString(
			string status
		) {
			switch (status) {
				case StripePaymentIntentStatus.Succeeded:
					return SubscriptionPaymentStatus.Succeeded;
				case StripePaymentIntentStatus.RequiresAction:
					return SubscriptionPaymentStatus.RequiresConfirmation;
				default:
					return SubscriptionPaymentStatus.Failed;
			}
		}
	}
}