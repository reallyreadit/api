using System;

namespace api.DataAccess.Models {
	public class NotificationEvent {
		public long Id { get; set; }
		public DateTime DateCreated { get; set; }
		public NotificationEventType Type { get; set; }
		public long? BulkEmailAuthorId { get; set; }
		public string BulkEmailSubject { get; set; }
		public string BulkEmailBody { get; set; }
		public BulkEmailSubscriptionStatusFilter? BulkEmailSubscriptionStatusFilter { get; set; }
		public bool? BulkEmailFreeForLifeFilter { get; set; }
	}
}