using Amazon;
using api.Messaging;

namespace api.Configuration {
	public class EmailOptions {
		public EmailMailboxOptions From { get; set; }
		public EmailDeliveryMethod DeliveryMethod { get; set; }
		public string AmazonSesRegionEndpoint { get; set; }
		public SmtpServerOptions SmtpServer { get; set; }
		public string EncryptionKey { get; set; }
	}
}