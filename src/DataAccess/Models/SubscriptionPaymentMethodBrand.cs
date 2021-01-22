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