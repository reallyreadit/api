using System.Text.Json.Serialization;

namespace api.Subscriptions {
	/// <summary>Auto-renewable subscription renewal that is open or has failed in the past.</summary>
	/// <remarks>
	/// <para>In the JSON file, <c>pending_renewal_info</c> is an array in which each element contains the pending renewal information for each auto-renewable subscription identified by the <c>product_id</c>. A pending renewal may refer to a renewal that is scheduled in the future or a renewal that failed in the past for some reason. It is only returned for app receipts that contain auto-renewable subscriptions.</para>
	/// <para>You can use this value to get critical information about any pending renewal transactions for an auto-renewable subscription.</para>
	/// </remarks>
	class AppStorePendingRenewalInfo {
		/// <summary>The current renewal preference for the auto-renewable subscription. The value for this key corresponds to the <c>productIdentifier</c> property of the product that the customer’s subscription renews. This field is only present if the user downgrades or crossgrades to a subscription of a different duration for the subsequent subscription period.</summary>
		[JsonPropertyName("auto_renew_product_id")]
		public string AutoRenewProductId { get; set; }

		/// <summary>The current renewal status for the auto-renewable subscription. See <see href="https://developer.apple.com/documentation/appstorereceipts/auto_renew_status">auto_renew_status</c> for more information.</summary>
		[JsonPropertyName("auto_renew_status")]
		public string AutoRenewStatus { get; set; }

		/// <summary>The reason a subscription expired. This field is only present for a receipt that contains an expired auto-renewable subscription.</summary>
		[JsonPropertyName("expiration_intent")]
		public string ExpirationIntent { get; set; }

		/// <summary>The time at which the grace period for subscription renewals expires, in a date-time format similar to the ISO 8601.</summary>
		[JsonPropertyName("grace_period_expires_date")]
		public string GracePeriodExpiresDate { get; set; }

		/// <summary>The time at which the grace period for subscription renewals expires, in UNIX epoch time format, in milliseconds. This key is only present for apps that have Billing Grace Period enabled and when the user experiences a billing error at the time of renewal. Use this time format for processing dates.</summary>
		[JsonPropertyName("grace_period_expires_date_ms")]
		public string GracePeriodExpiresDateMs { get; set; }

		/// <summary>The time at which the grace period for subscription renewals expires, in the Pacific Time zone.</summary>
		[JsonPropertyName("grace_period_expires_date_pst")]
		public string GracePeriodExpiresDatePst { get; set; }

		/// <summary>A flag that indicates Apple is attempting to renew an expired subscription automatically. This field is only present if an auto-renewable subscription is in the billing retry state. See <see href="https://developer.apple.com/documentation/appstorereceipts/is_in_billing_retry_period">is_in_billing_retry_period</see> for more information.</summary>
		[JsonPropertyName("is_in_billing_retry_period")]
		public string IsInBillingRetryPeriod { get; set; }

		/// <summary>The reference name of a subscription offer that you configured in App Store Connect. This field is present when a customer redeemed a subscription offer code. For more information about offer codes, see <see href="https://help.apple.com/app-store-connect/#/dev6a098e4b1">Set Up Offer Codes</see>, and <see href="https://developer.apple.com/documentation/storekit/in-app_purchase/subscriptions_and_offers/implementing_offer_codes_in_your_app">Implementing Offer Codes in Your App</see>.</summary>
		[JsonPropertyName("offer_code_ref_name")]
		public string OfferCodeRefName { get; set; }

		/// <summary>The transaction identifier of the original purchase.</summary>
		[JsonPropertyName("original_transaction_id")]
		public string OriginalTransactionId { get; set; }

		/// <summary>
		/// <para>The price consent status for a subscription price increase. This field is only present if the customer was notified of the price increase. The default value is "<c>0</c>" and changes to "<c>1</c>" if the customer consents.</para>
		/// <para>Possible values: <c>1</c>, <c>0</c></para>
		/// </summary>
		[JsonPropertyName("price_consent_status")]
		public string PriceConsentStatus { get; set; }

		/// <summary>The unique identifier of the product purchased. You provide this value when creating the product in App Store Connect, and it corresponds to the <c>productIdentifier</c> property of the <c>SKPayment</c> object stored in the transaction's payment property.</summary>
		[JsonPropertyName("product_id")]
		public string ProductId { get; set; }
	}
}