using System.Text.Json.Serialization;

namespace api.Subscriptions {
	/// <summary></summary>
	/// <remarks>https://developer.apple.com/documentation/appstorereceipts/responsebody/receipt/in_app</remarks>
	public class AppStoreInAppPurchaseReceipt {
		/// <summary>The time Apple customer support canceled a transaction, in a date-time format similar to the ISO 8601. This field is only present for refunded transactions.</summary>
		[JsonPropertyName("cancellation_date")]
		public string CancellationDate { get; set; }

		/// <summary>The time Apple customer support canceled a transaction, or the time an auto-renewable subscription plan was upgraded, in UNIX epoch time format, in milliseconds. This field is only present for refunded transactions. Use this time format for processing dates. See <see href="https://developer.apple.com/documentation/appstorereceipts/cancellation_date_ms"><c>cancellation_date_ms</c></see> for more information.</summary>
		[JsonPropertyName("cancellation_date_ms")]
		public string CancellationDateMs { get; set; }

		/// <summary>The time Apple customer support canceled a transaction, in the Pacific Time zone. This field is only present for refunded transactions.</summary>
		[JsonPropertyName("cancellation_date_pst")]
		public string CancellationDatePst { get; set; }

		/// <summary>
		/// <para>The reason for a refunded transaction. When a customer cancels a transaction, the App Store gives them a refund and provides a value for this key. A value of “<c>1</c>” indicates that the customer canceled their transaction due to an actual or perceived issue within your app. A value of “<c>0</c>” indicates that the transaction was canceled for another reason; for example, if the customer made the purchase accidentally.</para>
		/// <para>Possible values: <c>1</c>, <c>0</c></para>
		/// </summary>
		[JsonPropertyName("cancellation_reason")]
		public string CancellationReason { get; set; }

		/// <summary>The time a subscription expires or when it will renew, in a date-time format similar to the ISO 8601.</summary>
		[JsonPropertyName("expires_date")]
		public string ExpiresDate { get; set; }

		/// <summary>The time a subscription expires or when it will renew, in UNIX epoch time format, in milliseconds. Use this time format for processing dates. See <see href="https://developer.apple.com/documentation/appstorereceipts/expires_date_ms"><c>expires_date_ms</c></see> for more information.</summary>
		[JsonPropertyName("expires_date_ms")]
		public string ExpiresDateMs { get; set; }

		/// <summary>The time a subscription expires or when it will renew, in the Pacific Time zone.</summary>
		[JsonPropertyName("expires_date_pst")]
		public string ExpiresDatePst { get; set; }

		/// <summary>
		/// <para>An indicator of whether an auto-renewable subscription is in the introductory price period. See <see href="https://developer.apple.com/documentation/appstorereceipts/is_in_intro_offer_period"><c>is_in_intro_offer_period</c></see> for more information.</para>
		/// <para>Possible values: <c>true</c>, <c>false</c></para>
		/// </summary>
		[JsonPropertyName("is_in_intro_offer_period")]
		public string IsInIntroOfferPeriod { get; set; }

		/// <summary>An indicator of whether a subscription is in the free trial period. See <see href="https://developer.apple.com/documentation/appstorereceipts/is_trial_period"><c>is_trial_period</c></see> for more information.</summary>
		[JsonPropertyName("is_trial_period")]
		public string IsTrialPeriod { get; set; }

		/// <summary>The time of the original app purchase, in a date-time format similar to ISO 8601.</summary>
		[JsonPropertyName("original_purchase_date")]
		public string OriginalPurchaseDate { get; set; }

		/// <summary>The time of the original app purchase, in UNIX epoch time format, in milliseconds. Use this time format for processing dates. For an auto-renewable subscription, this value indicates the date of the subscription’s initial purchase. The original purchase date applies to all product types and remains the same in all transactions for the same product ID. This value corresponds to the original transaction’s <c>transactionDate</c> property in StoreKit.</summary>
		[JsonPropertyName("original_purchase_date_ms")]
		public string OriginalPurchaseDateMs { get; set; }

		/// <summary>The time of the original app purchase, in the Pacific Time zone.</summary>
		[JsonPropertyName("original_purchase_date_pst")]
		public string OriginalPurchaseDatePst { get; set; }

		/// <summary>The transaction identifier of the original purchase. See <see href="https://developer.apple.com/documentation/appstorereceipts/original_transaction_id">original_transaction_id</see> for more information.</summary>
		[JsonPropertyName("original_transaction_id")]
		public string OriginalTransactionId { get; set; }

		/// <summary>The unique identifier of the product purchased. You provide this value when creating the product in App Store Connect, and it corresponds to the <c>productIdentifier</c> property of the <c>SKPayment</c> object stored in the transaction’s payment property.</summary>
		[JsonPropertyName("product_id")]
		public string ProductId { get; set; }

		/// <summary>The identifier of the subscription offer redeemed by the user. See <see href="https://developer.apple.com/documentation/appstorereceipts/promotional_offer_id"><c>promotional_offer_id</c></see> for more information.</summary>
		[JsonPropertyName("promotional_offer_id")]
		public string PromotionalOfferId { get; set; }

		/// <summary>The time the App Store charged the user’s account for a purchased or restored product, or the time the App Store charged the user’s account for a subscription purchase or renewal after a lapse, in a date-time format similar to ISO 8601.</summary>
		[JsonPropertyName("purchase_date")]
		public string PurchaseDate { get; set; }

		/// <summary>For consumable, non-consumable, and non-renewing subscription products, the time the App Store charged the user’s account for a purchased or restored product, in the UNIX epoch time format, in milliseconds. For auto-renewable subscriptions, the time the App Store charged the user’s account for a subscription purchase or renewal after a lapse, in the UNIX epoch time format, in milliseconds. Use this time format for processing dates.</summary>
		[JsonPropertyName("purchase_date_ms")]
		public string PurchaseDateMs { get; set; }

		/// <summary>The time the App Store charged the user’s account for a purchased or restored product, or the time the App Store charged the user’s account for a subscription purchase or renewal after a lapse, in the Pacific Time zone.</summary>
		[JsonPropertyName("purchase_date_pst")]
		public string PurchaseDatePst { get; set; }

		/// <summary>The number of consumable products purchased. This value corresponds to the <c>quantity</c> property of the <c>SKPayment</c> object stored in the transaction’s payment property. The value is usually “<c>1</c>” unless modified with a mutable payment. The maximum value is <c>10</c>.</summary>
		[JsonPropertyName("quantity")]
		public string Quantity { get; set; }

		/// <summary>A unique identifier for a transaction such as a purchase, restore, or renewal. See <see href="https://developer.apple.com/documentation/appstorereceipts/transaction_id"><c>transaction_id</c></see> for more information.</summary>
		[JsonPropertyName("transaction_id")]
		public string TransactionId { get; set; }

		/// <summary>A unique identifier for purchase events across devices, including subscription-renewal events. This value is the primary key for identifying subscription purchases.</summary>
		[JsonPropertyName("web_order_line_item_id")]
		public string WebOrderLineItemId { get; set; }
	}
}