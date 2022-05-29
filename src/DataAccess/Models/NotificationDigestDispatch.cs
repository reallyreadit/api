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

namespace api.DataAccess.Models {
	public class NotificationDigestDispatch<T> : NotificationEmailDispatch {
		public NotificationDigestDispatch(
			long receiptId,
			long userAccountId,
			string userName,
			string emailAddress,
			IList<T> items
		) {
			ReceiptId = receiptId;
			UserAccountId = userAccountId;
			UserName = userName;
			EmailAddress = emailAddress;
			Items = items;
		}
		public IList<T> Items { get; }
	}
}