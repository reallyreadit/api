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
	public enum SubscriptionPaymentMethodBrandClientValue {
		None = 0,
		Amex = 1,
		Diners = 2,
		Discover = 3,
		Jcb = 4,
		Mastercard = 5,
		Unionpay = 6,
		Visa = 7
	}
	public static class SubscriptionPaymentMethodBrandClientValueExtensions {
		public static SubscriptionPaymentMethodBrandClientValue FromSubscriptionPaymentMethodBrand(
			SubscriptionPaymentMethodBrand brand
		) {
			switch (brand) {
				case SubscriptionPaymentMethodBrand.None:
				case SubscriptionPaymentMethodBrand.Unknown:
					return SubscriptionPaymentMethodBrandClientValue.None;
				case SubscriptionPaymentMethodBrand.Amex:
					return SubscriptionPaymentMethodBrandClientValue.Amex;
				case SubscriptionPaymentMethodBrand.Diners:
					return SubscriptionPaymentMethodBrandClientValue.Diners;
				case SubscriptionPaymentMethodBrand.Discover:
					return SubscriptionPaymentMethodBrandClientValue.Discover;
				case SubscriptionPaymentMethodBrand.Jcb:
					return SubscriptionPaymentMethodBrandClientValue.Jcb;
				case SubscriptionPaymentMethodBrand.Mastercard:
					return SubscriptionPaymentMethodBrandClientValue.Mastercard;
				case SubscriptionPaymentMethodBrand.Unionpay:
					return SubscriptionPaymentMethodBrandClientValue.Unionpay;
				case SubscriptionPaymentMethodBrand.Visa:
					return SubscriptionPaymentMethodBrandClientValue.Visa;
			}
			throw new ArgumentOutOfRangeException($"Unexpected value for brand: {brand}");
		}
	}
}