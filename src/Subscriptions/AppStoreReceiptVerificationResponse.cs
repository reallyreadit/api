// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System.Text.Json.Serialization;

namespace api.Subscriptions {
	/// <summary>The JSON data returned in the response from the App Store.</summary>
	public class AppStoreReceiptVerificationResponse : IAppStoreUnifiedReceipt {
		/// <summary>
		/// <para>The environment for which the receipt was generated.</para>
		/// <para>Possible values: <c>Sandbox</c>, <c>Production</c></para>
		/// </summary>
		[JsonPropertyName("environment")]
		public string Environment { get; set; }

		/// <summary>An indicator that an error occurred during the request. A value of <c>1</c> indicates a temporary issue; retry validation for this receipt at a later time. A value of <c>0</c> indicates an unresolvable issue; do not retry validation for this receipt. Only applicable to status codes <c>21100</c>-<c>21199</c>.</summary>
		[JsonPropertyName("is-retryable")]
		public int? IsRetryable { get; set; }

		/// <summary>The latest Base64 encoded app receipt. Only returned for receipts that contain auto-renewable subscriptions.</summary>
		[JsonPropertyName("latest_receipt")]
		public string LatestReceipt { get; set; }

		/// <summary>An array that contains all in-app purchase transactions. This excludes transactions for consumable products that have been marked as finished by your app. Only returned for receipts that contain auto-renewable subscriptions.</summary>
		[JsonPropertyName("latest_receipt_info")]
		public AppStoreLatestReceiptInfo[] LatestReceiptInfo { get; set; }

		/// <summary>In the JSON file, an array where each element contains the pending renewal information for each auto-renewable subscription identified by the <c>product_id</c>. Only returned for app receipts that contain auto-renewable subscriptions.</summary>
		[JsonPropertyName("pending_renewal_info")]
		public AppStorePendingRenewalInfo[] PendingRenewalInfo { get; set; }

		/// <summary>A JSON representation of the receipt that was sent for verification.</summary>
		[JsonPropertyName("receipt")]
		public AppStoreReceipt Receipt { get; set; }

		/// <summary>Either <c>0</c> if the receipt is valid, or a status code if there is an error. The status code reflects the status of the app receipt as a whole. See <see href="https://developer.apple.com/documentation/appstorereceipts/status">status</see> for possible status codes and descriptions.</summary>
		[JsonPropertyName("status")]
		public long Status { get; set; }
	}
}