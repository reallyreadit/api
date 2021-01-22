using System;
using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public enum SubscriptionPaymentMethodWalletClientValue {
		None = 0,
		AmexExpressCheckout = 1,
		ApplePay = 2,
		GooglePay = 3,
		Masterpass = 4,
		SamsungPay = 5,
		VisaCheckout = 6
	}
	public static class SubscriptionPaymentMethodWalletClientValueExtensions {
		public static SubscriptionPaymentMethodWalletClientValue FromSubscriptionPaymentMethodWallet(
			SubscriptionPaymentMethodWallet wallet
		) {
			switch (wallet) {
				case SubscriptionPaymentMethodWallet.None:
				case SubscriptionPaymentMethodWallet.Unknown:
					return SubscriptionPaymentMethodWalletClientValue.None;
				case SubscriptionPaymentMethodWallet.AmexExpressCheckout:
					return SubscriptionPaymentMethodWalletClientValue.AmexExpressCheckout;
				case SubscriptionPaymentMethodWallet.ApplePay:
					return SubscriptionPaymentMethodWalletClientValue.ApplePay;
				case SubscriptionPaymentMethodWallet.GooglePay:
					return SubscriptionPaymentMethodWalletClientValue.GooglePay;
				case SubscriptionPaymentMethodWallet.Masterpass:
					return SubscriptionPaymentMethodWalletClientValue.Masterpass;
				case SubscriptionPaymentMethodWallet.SamsungPay:
					return SubscriptionPaymentMethodWalletClientValue.SamsungPay;
				case SubscriptionPaymentMethodWallet.VisaCheckout:
					return SubscriptionPaymentMethodWalletClientValue.VisaCheckout;
			}
			throw new ArgumentOutOfRangeException($"Unexpected value for wallet: {wallet}");
		}
	}
}