using System;
using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public enum SubscriptionProviderClientValue {
		Apple = 1,
		Stripe = 2
	}
	public static class SubscriptionProviderClientValueExtensions {
		public static SubscriptionProviderClientValue FromSubscriptionProvider(
			SubscriptionProvider provider
		) {
			switch (provider) {
				case SubscriptionProvider.Apple:
					return SubscriptionProviderClientValue.Apple;
				case SubscriptionProvider.Stripe:
					return SubscriptionProviderClientValue.Stripe;
			}
			throw new ArgumentOutOfRangeException($"Unexpected value for provider: {provider}");
		}
		public static SubscriptionProvider ToSubscriptionProvider(
			this SubscriptionProviderClientValue provider
		) {
			switch (provider) {
				case SubscriptionProviderClientValue.Apple:
					return SubscriptionProvider.Apple;
				case SubscriptionProviderClientValue.Stripe:
					return SubscriptionProvider.Stripe;
			}
			throw new ArgumentOutOfRangeException($"Unexpected value for provider: {provider}");
		}
	}
}