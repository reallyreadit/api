namespace api.Notifications {
	public class ApnsNotification {
		public ApnsNotification(
			ApnsPayload payload,
			string[] tokens
		) {
			Payload = payload;
			Tokens = tokens;
		}
		public ApnsNotification(
			long receiptId,
			ApnsPayload payload,
			string[] tokens
		) {
			ReceiptId = receiptId;
			Payload = payload;
			Tokens = tokens;
		}
		public long? ReceiptId { get; }
		public ApnsPayload Payload { get; }
		public string[] Tokens { get; }
	}
}