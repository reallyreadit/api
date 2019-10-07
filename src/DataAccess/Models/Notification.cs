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