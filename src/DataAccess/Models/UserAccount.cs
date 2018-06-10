using System;
using System.Runtime.Serialization;
using api.Messaging;

namespace api.DataAccess.Models {
	public class UserAccount : IEmailRecipient {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		[IgnoreDataMember]
		public byte[] PasswordHash { get; set; }
		[IgnoreDataMember]
		public byte[] PasswordSalt { get; set; }
		public bool ReceiveReplyEmailNotifications { get; set; }
		public bool ReceiveReplyDesktopNotifications { get; set; }
		public DateTime LastNewReplyAck { get; set; }
		public DateTime LastNewReplyDesktopNotification { get; set; }
		public DateTime DateCreated { get; set; }
		public UserAccountRole Role { get; set; }
		public bool ReceiveWebsiteUpdates { get; set; }
		public bool ReceiveSuggestedReadings { get; set; }
		public bool IsEmailConfirmed { get; set; }
		public long? TimeZoneId { get; set; }
		public string TimeZoneName { get; set; }
		public string TimeZoneDisplayName { get; set; }

		string IEmailRecipient.EmailAddress => Email;
		bool IEmailRecipient.IsEmailAddressConfirmed => IsEmailConfirmed;
	}
}