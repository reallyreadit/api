using System;
using System.Text.Json.Serialization;
using api.Notifications;
using api.Messaging;

namespace api.DataAccess.Models {
	public class UserAccount :  IAlertStatus, IEmailRecipient {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		[JsonIgnore]
		public byte[] PasswordHash { get; set; }
		[JsonIgnore]
		public byte[] PasswordSalt { get; set; }
		public DateTime DateCreated { get; set; }
		public UserAccountRole Role { get; set; }
		public long? TimeZoneId { get; set; }
		public bool IsEmailConfirmed { get; set; }
		public bool AotdAlert { get; set; }
		public int ReplyAlertCount { get; set; }
		public int LoopbackAlertCount { get; set; }
		public int PostAlertCount { get; set; }
		public int FollowerAlertCount { get; set; }
		public bool HasLinkedTwitterAccount { get; set; }
		public bool IsPasswordSet => (
			PasswordHash != null &&
			PasswordSalt != null
		);
		string IEmailRecipient.EmailAddress => Email;
		bool IEmailRecipient.IsEmailAddressConfirmed => IsEmailConfirmed;
	}
}