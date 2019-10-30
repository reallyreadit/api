using System.Collections.Generic;
using api.Notifications;

namespace api.DataAccess.Models {
	public class NotificationDigestDispatch<T> : INotificationDispatch {
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
		public long ReceiptId { get; }
		public long UserAccountId { get; }
		public string UserName { get; }
		public string EmailAddress { get; }
		public IList<T> Items { get; }
	}
}