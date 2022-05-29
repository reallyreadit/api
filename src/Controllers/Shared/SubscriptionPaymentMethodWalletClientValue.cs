// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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