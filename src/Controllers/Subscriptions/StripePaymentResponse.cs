using System;
using api.Controllers.Shared;
using api.Subscriptions;

namespace api.Controllers.Subscriptions {
	public enum StripePaymentResponseType {
		Succeeded = 1,
		RequiresConfirmation = 2,
		Failed = 3
	}
	public abstract class StripePaymentResponse {
		public static StripePaymentResponse FromPaymentIntent(
			Stripe.PaymentIntent paymentIntent,
			SubscriptionStatusClientModel subscriptionStatus
		) {
			switch (paymentIntent.Status) {
				case StripePaymentIntentStatus.RequiresPaymentMethod:
					string errorMessage;
					switch (paymentIntent.LastPaymentError?.Code) {
						case StripeErrorCode.PaymentIntentAuthenticationError:
							errorMessage = "We were unable to authenticate your payment method.";
							break;
						default:
							errorMessage = paymentIntent.LastPaymentError?.Message;
							break;
					}
					return new StripePaymentFailedResponse(
						subscriptionStatus: subscriptionStatus,
						errorMessage: errorMessage
					);
				case StripePaymentIntentStatus.RequiresAction:
					return new StripePaymentRequiresConfirmationResponse(
						subscriptionStatus: subscriptionStatus,
						clientSecret: paymentIntent.ClientSecret,
						invoiceId: paymentIntent.InvoiceId
					);
				case StripePaymentIntentStatus.Succeeded:
					return new StripePaymentSucceededResponse(subscriptionStatus);
				default:
					throw new ArgumentException($"Unexpected payment intent status: {paymentIntent.Status}.");
			}
		}
		public StripePaymentResponse(
			StripePaymentResponseType type,
			SubscriptionStatusClientModel subscriptionStatus
		) {
			Type = type;
			SubscriptionStatus = subscriptionStatus;
		}
		public StripePaymentResponseType Type { get; }
		public object SubscriptionStatus { get; }
	}
	public class StripePaymentSucceededResponse :
		StripePaymentResponse
	{
		public StripePaymentSucceededResponse(
			SubscriptionStatusClientModel subscriptionStatus
		) :
			base(
				StripePaymentResponseType.Succeeded,
				subscriptionStatus
			)
		{
		}
	}
	public class StripePaymentRequiresConfirmationResponse :
		StripePaymentResponse
	{
		public StripePaymentRequiresConfirmationResponse(
			string clientSecret,
			string invoiceId,
			SubscriptionStatusClientModel subscriptionStatus
		) :
			base(
				StripePaymentResponseType.RequiresConfirmation,
				subscriptionStatus
			)
		{
			ClientSecret = clientSecret;
			InvoiceId = invoiceId;
		}
		public string ClientSecret { get; }
		public string InvoiceId { get; }
	}
	public class StripePaymentFailedResponse :
		StripePaymentResponse
	{
		public StripePaymentFailedResponse(
			string errorMessage,
			SubscriptionStatusClientModel subscriptionStatus
		) :
			base(
				StripePaymentResponseType.Failed,
				subscriptionStatus
			)
		{
			ErrorMessage = errorMessage;
		}
		public string ErrorMessage { get; }
	}
}