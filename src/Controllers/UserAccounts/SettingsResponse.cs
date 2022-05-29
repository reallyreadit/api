// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using api.Authentication;
using api.Controllers.Shared;
using api.DataAccess.Models;

namespace api.Controllers.UserAccounts {
	public class SettingsResponse {
		public SettingsResponse(
			DisplayPreference displayPreference,
			int userCount,
			NotificationPreference notificationPreference,
			string timeZoneDisplayName,
			IEnumerable<AuthServiceAccountAssociation> authServiceAccounts,
			SubscriptionStatusClientModel subscriptionStatus,
			SubscriptionPaymentMethodClientModel subscriptionPaymentMethod,
			AuthorProfileClientModel authorProfile,
			PayoutAccountClientModel payoutAccount
		) {
			DisplayPreference = displayPreference;
			UserCount = userCount;
			NotificationPreference = notificationPreference;
			TimeZoneDisplayName = timeZoneDisplayName;
			AuthServiceAccounts = authServiceAccounts;
			SubscriptionStatus = subscriptionStatus;
			SubscriptionPaymentMethod = subscriptionPaymentMethod;
			AuthorProfile = authorProfile;
			PayoutAccount = payoutAccount;
		}
		public DisplayPreference DisplayPreference { get; }
		public int UserCount { get; }
		public NotificationPreference NotificationPreference { get; }
		public string TimeZoneDisplayName { get; }
		public IEnumerable<AuthServiceAccountAssociation> AuthServiceAccounts { get; }
		public object SubscriptionStatus { get; }
		public SubscriptionPaymentMethodClientModel SubscriptionPaymentMethod { get; }
		public AuthorProfileClientModel AuthorProfile { get; }
		public PayoutAccountClientModel PayoutAccount { get; }
	}
}