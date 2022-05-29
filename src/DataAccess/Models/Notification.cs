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

namespace api.DataAccess.Models {
	public class Notification {
		public long EventId { get; set; }
		public DateTime DateCreated { get; set; }
		public NotificationEventType EventType { get; set; }
		public long[] ArticleIds { get; set; }
		public long[] CommentIds { get; set; }
		public long[] SilentPostIds { get; set; }
		public long[] FollowingIds { get; set; }
		public long ReceiptId { get; set; }
		public long UserAccountId { get; set; }
		public DateTime? DateAlertCleared { get; set; }
		public bool ViaEmail { get; set; }
		public bool ViaExtension { get; set; }
		public bool ViaPush { get; set; }
	}
}