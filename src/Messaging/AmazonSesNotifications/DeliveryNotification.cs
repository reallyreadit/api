namespace api.Messaging.AmazonSesNotifications {
	public class DeliveryNotification {
		public Bounce Bounce { get; set; }
		public Complaint Complaint { get; set; }
		public DeliveryMail Mail { get; set; }
		public string NotificationType { get; set; }
	}
}