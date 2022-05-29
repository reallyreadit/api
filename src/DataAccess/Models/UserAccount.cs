// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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
		[JsonIgnore]
		public DateTime? DateDeleted { get; set; }
		public DateTime? DateOrientationCompleted { get; set; }
		[JsonIgnore]
		public DateTime? SubscriptionEndDate { get; set; }
		public bool IsPasswordSet => (
			PasswordHash != null &&
			PasswordSalt != null
		);
		string IEmailRecipient.EmailAddress => Email;
		bool IEmailRecipient.IsEmailAddressConfirmed => IsEmailConfirmed;
	}
}