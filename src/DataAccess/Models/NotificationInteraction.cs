using System;

namespace api.DataAccess.Models {
	public class NotificationInteraction {
		public long Id { get; set; }
		public long ReceiptId { get; set; }
		public NotificationChannel Channel { get; set; }
		public NotificationAction Action { get; set; }
		public DateTime DateCreated { get; set; }
		public string Url { get; set; }
		public long? ReplyId { get; set; }
	}
}