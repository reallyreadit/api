namespace api.Messaging.AmazonSesNotifications {
	public class DeliveryMail : Mail {
		public string SendingAccountId { get; set; }
		public string SourceArn { get; set; }
		public string SourceIp { get; set; }
	}
}