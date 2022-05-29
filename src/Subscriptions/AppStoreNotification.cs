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
	/// <summary>The JSON data sent in the server notification from the App Store.</summary>
	/// <remarks>Use the information in the response body to react quickly to changes in your users’ subscription states. The fields available in any one notification sent to your server are dependent on the <c>notification_type</c>, which indicates the event that triggered the notification.</remarks>
	public class AppStoreNotification {
		/// <summary>An identifier that App Store Connect generates and the App Store uses to uniquely identify the auto-renewable subscription that the user’s subscription renews. Treat this value as a 64-bit integer.</summary>
		[JsonPropertyName("auto_renew_adam_id")]
		public string AutoRenewAdamId { get; set; }

		/// <summary>The product identifier of the auto-renewable subscription that the user’s subscription renews.</summary>
		[JsonPropertyName("auto_renew_product_id")]
		public string AutoRenewProductId { get; set; }

		/// <summary>
		/// <para>The current renewal status for an auto-renewable subscription product. Note that these values are different from those of the <c>auto_renew_status</c> in the receipt.</para>
		/// <para>Possible values: <c>true</c>, <c>false</c></para>
		/// </summary>
		[JsonPropertyName("auto_renew_status")]
		public string AutoRenewStatus { get; set; }

		/// <summary>The time at which the user turned on or off the renewal status for an auto-renewable subscription, in a date-time format similar to the ISO 8601 standard.</summary>
		[JsonPropertyName("auto_renew_status_change_date")]
		public string AutoRenewStatusChangeDate { get; set; }

		/// <summary>The time at which the user turned on or off the renewal status for an auto-renewable subscription, in UNIX epoch time format, in milliseconds. Use this time format to process dates.</summary>
		[JsonPropertyName("auto_renew_status_change_date_ms")]
		public string AutoRenewStatusChangeDateMs { get; set; }

		/// <summary>The time at which the user turned on or off the renewal status for an auto-renewable subscription, in the Pacific time zone.</summary>
		[JsonPropertyName("auto_renew_status_change_date_pst")]
		public string AutoRenewStatusChangeDatePst { get; set; }

		/// <summary>
		/// <para>The environment for which App Store generated the receipt.</para>
		/// <para>Possible values: <c>Sandbox</c>, <c>PROD</c></para>
		/// </summary>
		[JsonPropertyName("environment")]
		public string Environment { get; set; }

		/// <summary>The reason a subscription expired. This field is only present for an expired auto-renewable subscription. See <see href="https://developer.apple.com/documentation/appstorereceipts/expiration_intent">expiration_intent</see> for more information.</summary>
		[JsonPropertyName("expiration_intent")]
		public int? ExpirationIntent { get; set; }

		/// <summary>The subscription event that triggered the notification.</summary>
		[JsonPropertyName("notification_type")]
		public string NotificationType { get; set; }

		/// <summary>The same value as the shared secret you submit in the <c>password</c> field of the requestBody when validating receipts.</summary>
		[JsonPropertyName("password")]
		public string Password { get; set; }

		/// <summary>An object that contains information about the most-recent, in-app purchase transactions for the app.</summary>
		[JsonPropertyName("unified_receipt")]
		public AppStoreUnifiedReceipt UnifiedReceipt { get; set; }

		/// <summary>A string that contains the app bundle ID.</summary>
		[JsonPropertyName("bid")]
		public string Bid { get; set; }

		/// <summary>A string that contains the app bundle version.</summary>
		[JsonPropertyName("bvrs")]
		public string Bvrs { get; set; }
	}
}