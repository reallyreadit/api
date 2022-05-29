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
	public class BulkMailing {
		public long Id { get; set; }
		public DateTime DateSent { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public string Type { get; set; }
		public BulkEmailSubscriptionStatusFilter? SubscriptionStatusFilter { get; set; }
		public bool? FreeForLifeFilter { get; set; }
		public DateTime? UserCreatedAfterFilter { get; set; }
		public DateTime? UserCreatedBeforeFilter { get; set; }
		public string UserAccount { get; set; }
		public int RecipientCount { get; set; }
	}
}