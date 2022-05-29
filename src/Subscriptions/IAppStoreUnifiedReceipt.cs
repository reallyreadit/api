// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

namespace api.Subscriptions {
	public interface IAppStoreUnifiedReceipt {
		/// <summary>
		/// <para>The environment for which App Store generated the receipt.</para>
		/// <para>Possible values: <c>Sandbox</c>, <c>Production</c></para>
		/// </summary>
		string Environment { get; set; }

		/// <summary>The latest Base64-encoded app receipt.</summary>
		string LatestReceipt { get; set; }

		/// <summary>An array that contains in-app purchase transactions of the decoded value in <c>latest_receipt</c>. This array excludes transactions for consumable products your app has marked as finished.</summary>
		AppStoreLatestReceiptInfo[] LatestReceiptInfo { get; set; }

		/// <summary>An array where each element contains the pending renewal information for each auto-renewable subscription identified in <c>product_id</c>.</summary>
		AppStorePendingRenewalInfo[] PendingRenewalInfo { get; set; }

		/// <summary>
		/// <para>The status code, where <c>0</c> indicates that the receipt is valid.</para>
		/// <para>Value: <c>0</c></para>
		/// </summary>
		long Status { get; set; }
	}
}