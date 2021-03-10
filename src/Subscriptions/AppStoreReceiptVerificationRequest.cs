using System.Text.Json.Serialization;

namespace api.Subscriptions {
	/// <summary>The JSON contents you submit with the request to the App Store</summary>
	/// <remarks>To receive a decoded receipt for validation, send a request with the encoded receipt data and app password to the App Store. For auto-renewable subscriptions, optionally include an exclusion flag. Send this JSON data using the HTTP POST request method.</remarks>
	public class AppStoreReceiptVerificationRequest {
		/// <summary>(Required) The Base64-encoded receipt data.</summary>
		[JsonPropertyName("receipt-data")]
		public string ReceiptData { get; set; }

		/// <summary>(Required) Your app’s shared secret, which is a hexadecimal string.</summary>
		[JsonPropertyName("password")]
		public string Password { get; set; }

		/// <summary>Set this value to <c>true</c> for the response to include only the latest renewal transaction for any subscriptions.</summary>
		/// <remarks>Use this field only for app receipts that contain auto-renewable subscriptions.</remarks>
		[JsonPropertyName("exclude-old-transactions")]
		public bool ExcludeOldTransactions { get; set; }
	}
}