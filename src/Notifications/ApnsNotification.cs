namespace api.Notifications {
	public class ApnsNotification {
		public ApnsNotification(
			ApnsPayload payload,
			string[] tokens
		) {
			Payload = payload;
			Tokens = tokens;
		}
		public ApnsPayload Payload { get; }
		public string[] Tokens { get; }
	}
}