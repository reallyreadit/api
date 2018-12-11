using System;

namespace api.Messaging.AmazonSesNotifications {
	public class Complaint {
		public DateTime ArrivalDate { get; set; }
		public Recipient[] ComplainedRecipients { get; set; } = new Recipient[0];
		public string ComplaintFeedbackType { get; set; }
		public string FeedbackId { get; set; }
		public DateTime Timestamp { get; set; }
		public string UserAgent { get; set; }
	}
}