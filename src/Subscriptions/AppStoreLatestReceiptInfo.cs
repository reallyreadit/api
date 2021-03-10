using System.Text.Json.Serialization;

namespace api.Subscriptions {
	/// <summary></summary>
	/// <remarks>https://developer.apple.com/documentation/appstorereceipts/responsebody/latest_receipt_info</remarks>
	/// <remarks>https://developer.apple.com/documentation/appstoreservernotifications/unified_receipt/latest_receipt_info</remarks>
	public class AppStoreLatestReceiptInfo : AppStoreInAppPurchaseReceipt {
		/// <summary>
		/// <para>An indicator that a subscription has been canceled due to an upgrade. This field is only present for upgrade transactions.</para>
		/// <para>Value: <c>true</c></para>
		/// </summary>
		[JsonPropertyName("is_upgraded")]
		public string IsUpgraded { get; set; }

		/// <summary>The reference name of a subscription offer that you configured in App Store Connect. This field is present when a customer redeemed a subscription offer code. For more information about offer codes, see <see href="https://help.apple.com/app-store-connect/#/dev6a098e4b1">Set Up Offer Codes</see>, and <see href="https://developer.apple.com/documentation/storekit/in-app_purchase/subscriptions_and_offers/implementing_offer_codes_in_your_app">Implementing Offer Codes in Your App</see>.</summary>
		[JsonPropertyName("offer_code_ref_name")]
		public string OfferCodeRefName { get; set; }

		/// <summary>The identifier of the subscription group to which the subscription belongs. The value for this field is identical to the <c>subscriptionGroupIdentifier</c> property in <c>SKProduct</c>.</summary>
		[JsonPropertyName("subscription_group_identifier")]
		public string SubscriptionGroupIdentifier { get; set; }
	}
}