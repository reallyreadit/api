using System;

namespace api.Messaging.AmazonSesNotifications {
	public class Mail {
		public CommonHeaders CommonHeaders { get; set; } = new CommonHeaders();
		public string[] Destination { get; set; } = new string[0];
		public Header[] Headers { get; set; } = new Header[0];
		public bool HeadersTruncated { get; set; }
		public string MessageId { get; set; }
		public string Source { get; set; }
		public DateTime Timestamp { get; set; }
	}
}