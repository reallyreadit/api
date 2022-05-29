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
	public enum SubscriptionPaymentMethodBrand {
		None,
		Unknown,
		Amex,
		Diners,
		Discover,
		Jcb,
		Mastercard,
		Unionpay,
		Visa
	}
	public static class SubscriptionPaymentMethodBrandExtensions {
		public static SubscriptionPaymentMethodBrand FromStripePaymentMethodBrandString(
			string brand
		) {
			switch (brand) {
				case StripePaymentMethodBrand.Amex:
					return SubscriptionPaymentMethodBrand.Amex;
				case StripePaymentMethodBrand.Diners:
					return SubscriptionPaymentMethodBrand.Diners;
				case StripePaymentMethodBrand.Discover:
					return SubscriptionPaymentMethodBrand.Discover;
				case StripePaymentMethodBrand.Jcb:
					return SubscriptionPaymentMethodBrand.Jcb;
				case StripePaymentMethodBrand.Mastercard:
					return SubscriptionPaymentMethodBrand.Mastercard;
				case StripePaymentMethodBrand.Unionpay:
					return SubscriptionPaymentMethodBrand.Unionpay;
				case StripePaymentMethodBrand.Visa:
					return SubscriptionPaymentMethodBrand.Visa;
				case StripePaymentMethodBrand.Unknown:
					return SubscriptionPaymentMethodBrand.Unknown;
			}
			return SubscriptionPaymentMethodBrand.None;
		}
	}
}