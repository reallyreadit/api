namespace api.Messaging.AmazonSesNotifications {
	public class Notification {
		public Bounce Bounce { get; set; }
		public Complaint Complaint { get; set; }
		public Mail Mail { get; set; }
		public string NotificationType { get; set; }
	}
}