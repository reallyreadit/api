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