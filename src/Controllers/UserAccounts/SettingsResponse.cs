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
			SubscriptionPaymentMethodClientModel subscriptionPaymentMethod
		) {
			DisplayPreference = displayPreference;
			UserCount = userCount;
			NotificationPreference = notificationPreference;
			TimeZoneDisplayName = timeZoneDisplayName;
			AuthServiceAccounts = authServiceAccounts;
			SubscriptionStatus = subscriptionStatus;
			SubscriptionPaymentMethod = subscriptionPaymentMethod;
		}
		public DisplayPreference DisplayPreference { get; }
		public int UserCount { get; }
		public NotificationPreference NotificationPreference { get; }
		public string TimeZoneDisplayName { get; }
		public IEnumerable<AuthServiceAccountAssociation> AuthServiceAccounts { get; }
		public object SubscriptionStatus { get; }
		public SubscriptionPaymentMethodClientModel SubscriptionPaymentMethod { get; }
	}
}