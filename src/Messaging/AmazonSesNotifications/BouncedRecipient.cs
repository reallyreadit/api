namespace api.Messaging.AmazonSesNotifications {
	public class BouncedRecipient : Recipient {
		public string Action { get; set; }
		public string DiagnosticCode { get; set; }
		public string Status { get; set; }
	}
}