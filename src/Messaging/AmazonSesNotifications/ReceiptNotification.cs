namespace api.Messaging.AmazonSesNotifications {
	public class ReceiptNotification {
		public string Content { get; set; }
		public Mail Mail { get; set; }
		public string NotificationType { get; set; }
	}
}