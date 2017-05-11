using Amazon;
using api.Messaging;

namespace api.Configuration {
	public class EmailOptions {
		public string FromName { get; set; }
		public string FromAddress { get; set; }
		public EmailDeliveryMethod DeliveryMethod { get; set; }
		public string AmazonSesRegionEndpoint { get; set; }
		public SmtpServerOptions SmtpServer { get; set; }
	}
}