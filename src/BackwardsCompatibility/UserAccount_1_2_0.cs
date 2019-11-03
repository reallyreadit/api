using System;
using api.DataAccess.Models;

namespace api.BackwardsCompatibility {
	public class UserAccount_1_2_0 {
		public UserAccount_1_2_0(
			UserAccount user,
			NotificationPreferenceOptions preference = null,
			api.DataAccess.Models.TimeZone timeZone = null
		) {
			Id = user.Id;
			Name = user.Name;
			Email = user.Email;
			ReceiveReplyEmailNotifications = preference?.ReplyViaEmail ?? false;
			ReceiveReplyDesktopNotifications = preference?.ReplyViaExtension ?? false;
			LastNewReplyAck = new DateTime(1970, 1, 1);
			LastNewReplyDesktopNotification = new DateTime(1970, 1, 1);
			DateCreated = user.DateCreated;
			Role = user.Role;
			ReceiveWebsiteUpdates = preference?.CompanyUpdateViaEmail ?? false;
			ReceiveSuggestedReadings = preference?.AotdDigestViaEmail == NotificationEventFrequency.Weekly;
			IsEmailConfirmed = user.IsEmailConfirmed;
			TimeZoneId = user.TimeZoneId;
			TimeZoneName = timeZone?.Name;
			TimeZoneDisplayName = timeZone?.DisplayName;
		}
		public long Id { get; }
		public string Name { get; }
		public string Email { get; }
		public bool ReceiveReplyEmailNotifications { get; }
		public bool ReceiveReplyDesktopNotifications { get; }
		public DateTime LastNewReplyAck { get; }
		public DateTime LastNewReplyDesktopNotification { get; }
		public DateTime DateCreated { get; }
		public UserAccountRole Role { get; }
		public bool ReceiveWebsiteUpdates { get; }
		public bool ReceiveSuggestedReadings { get; }
		public bool IsEmailConfirmed { get; }
		public long? TimeZoneId { get;}
		public string TimeZoneName { get; }
		public string TimeZoneDisplayName { get; }
	}
}