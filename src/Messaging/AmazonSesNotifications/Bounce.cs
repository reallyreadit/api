using System;

namespace api.Messaging.AmazonSesNotifications {
	public class Bounce {
		public string BounceSubType { get; set; }
		public string BounceType { get; set; }
		public BouncedRecipient[] BouncedRecipients { get; set; } = new BouncedRecipient[0];
		public string FeedbackId { get; set; }
		public string RemoteMtaIp { get; set; }
		public string ReportingMta { get; set; }
		public DateTime Timestamp { get; set; }
	}
}