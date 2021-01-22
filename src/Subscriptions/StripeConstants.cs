using System;

namespace api.Subscriptions {
	public static class StripeErrorCode {
		public const string PaymentIntentAuthenticationError = "payment_intent_authentication_failure";
	}
	public static class StripeErrorType {
		public const string ApiConnectionError = "api_connection_error";
		public const string ApiError = "api_error";
		public const string AuthenticationError = "authentication_error";
		public const string CardError = "card_error";
		public const string IdempotencyError = "idempotency_error";
		public const string InvalidRequestError = "invalid_request_error";
		public const string RateLimitError = "rate_limit_error";
	}
	public static class StripeInvoiceExpandProperties {
		public const string PaymentIntent = "payment_intent";
	}
	public static class StripePaymentIntentExpandProperties {
		public const string Invoice = "invoice";
	}
	public static class StripePaymentIntentStatus {
		public const string RequiresPaymentMethod = "requires_payment_method";
		public const string RequiresConfirmation = "requires_confirmation";
		public const string RequiresAction = "requires_action";
		public const string Processing = "processing";
		public const string RequiresCapture = "requires_capture";
		public const string Cancelled = "canceled";
		public const string Succeeded = "succeeded";
	}
	public static class StripePaymentMethodBrand {
		public const string Amex = "amex";
		public const string Diners = "diners";
		public const string Discover = "discover";
		public const string Jcb = "jcb";
		public const string Mastercard = "mastercard";
		public const string Unionpay = "unionpay";
		public const string Unknown = "unknown";
		public const string Visa = "visa";
	}
	public static class StripePaymentMethodWallet {
		public const string AmexExpressCheckout = "amex_express_checkout";
		public const string ApplePay = "apple_pay";
		public const string GooglePay = "google_pay";
		public const string Masterpass = "masterpass";
		public const string SamsungPay = "samsung_pay";
		public const string VisaCheckout = "visa_checkout";
	}
	public static class StripeSubscriptionExpandProperties {
		public const string LatestInvoicePaymentIntent = "latest_invoice.payment_intent";
	}
	public static class StripeSubscriptionStatus {
		public const string Incomplete = "incomplete";
		public const string IncompleteExpired = "incomplete_expired";
		public const string Trialing = "trialing";
		public const string Active = "active";
		public const string PastDue = "past_due";
		public const string Cancelled = "canceled";
		public const string Unpaid = "unpaid";
	}
}