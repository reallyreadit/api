// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using api.Notifications;

namespace api.DataAccess.Models {
	public class NotificationAlertDispatch : IAlertStatus, INotificationDispatch {
		public long ReceiptId { get; set; }
		public bool ViaEmail { get; set; }
		public bool ViaPush { get; set; }
		public long UserAccountId { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
		public string[] PushDeviceTokens { get; set; }
		public bool AotdAlert { get; set; }
		public int ReplyAlertCount { get; set; }
		public int LoopbackAlertCount { get; set; }
		public int PostAlertCount { get; set; }
		public int FollowerAlertCount { get; set; }
	}
}