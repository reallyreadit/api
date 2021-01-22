using api.Subscriptions;

namespace api.DataAccess.Models {
	public enum SubscriptionPaymentMethodWallet {
		None,
		Unknown,
		AmexExpressCheckout,
		ApplePay,
		GooglePay,
		Masterpass,
		SamsungPay,
		VisaCheckout
	}
	public static class SubscriptionPaymentMethodWalletExtensions {
		public static SubscriptionPaymentMethodWallet FromStripePaymentMethodWalletString(
			string wallet
		) {
			if (wallet == null) {
				return SubscriptionPaymentMethodWallet.None;
			}
			switch (wallet) {
				case StripePaymentMethodWallet.AmexExpressCheckout:
					return SubscriptionPaymentMethodWallet.AmexExpressCheckout;
				case StripePaymentMethodWallet.ApplePay:
					return SubscriptionPaymentMethodWallet.ApplePay;
				case StripePaymentMethodWallet.GooglePay:
					return SubscriptionPaymentMethodWallet.GooglePay;
				case StripePaymentMethodWallet.Masterpass:
					return SubscriptionPaymentMethodWallet.Masterpass;
				case StripePaymentMethodWallet.SamsungPay:
					return SubscriptionPaymentMethodWallet.SamsungPay;
				case StripePaymentMethodWallet.VisaCheckout:
					return SubscriptionPaymentMethodWallet.VisaCheckout;
			}
			return SubscriptionPaymentMethodWallet.Unknown;
		}
	}
}