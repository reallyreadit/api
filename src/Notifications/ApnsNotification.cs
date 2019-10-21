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
			string collapseId,
			ApnsPayload payload,
			string[] tokens
		) {
			CollapseId = collapseId;
			Payload = payload;
			Tokens = tokens;
		}
		public string CollapseId { get; }
		public ApnsPayload Payload { get; }
		public string[] Tokens { get; }
	}
}