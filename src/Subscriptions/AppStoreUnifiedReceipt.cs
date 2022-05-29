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
	/// <summary>An object that contains information about the most-recent, in-app purchase transactions for the app.</summary>
	/// <remarks>https://developer.apple.com/documentation/appstoreservernotifications/unified_receipt</remarks>
	public class AppStoreUnifiedReceipt : IAppStoreUnifiedReceipt {
		/// <summary>
		/// <para>The environment for which App Store generated the receipt.</para>
		/// <para>Possible values: <c>Sandbox</c>, <c>Production</c></para>
		/// </summary>
		[JsonPropertyName("environment")]
		public string Environment { get; set; }

		/// <summary>The latest Base64-encoded app receipt.</summary>
		[JsonPropertyName("latest_receipt")]
		public string LatestReceipt { get; set; }

		/// <summary>An array that contains the latest 100 in-app purchase transactions of the decoded value in <c>latest_receipt</c>. This array excludes transactions for consumable products your app has marked as finished. The contents of this array are identical to those in responseBody.Latest_receipt_info in the verifyReceipt endpoint response for receipt validation.</summary>
		[JsonPropertyName("latest_receipt_info")]
		public AppStoreLatestReceiptInfo[] LatestReceiptInfo { get; set; }

		/// <summary>An array where each element contains the pending renewal information for each auto-renewable subscription identified in <c>product_id</c>. The contents of this array are identical to those in responseBody.Pending_renewal_info in the verifyReceipt endpoint response for receipt validation.</summary>
		[JsonPropertyName("pending_renewal_info")]
		public AppStorePendingRenewalInfo[] PendingRenewalInfo { get; set; }

		/// <summary>
		/// <para>The status code, where <c>0</c> indicates that the notification is valid.</para>
		/// <para>Value: <c>0</c></para>
		/// </summary>
		[JsonPropertyName("status")]
		public long Status { get; set; }
	}
}