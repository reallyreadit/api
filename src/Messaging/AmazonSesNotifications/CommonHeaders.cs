using System;

namespace api.Messaging.AmazonSesNotifications {
	public class CommonHeaders {
		public DateTime Date { get; set; }
		public string[] From { get; set; } = new string[0];
		public string MessageId { get; set; }
		public string Subject { get; set; }
		public string[] To { get; set; } = new string[0];
	}
}