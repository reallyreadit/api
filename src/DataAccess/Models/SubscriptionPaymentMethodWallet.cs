// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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