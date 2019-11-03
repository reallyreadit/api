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