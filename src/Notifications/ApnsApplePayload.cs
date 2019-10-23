namespace api.Notifications {
	public class ApnsApplePayload {
		public ApnsApplePayload(
			int badge
		) {
			Badge = badge;
		}
		public ApnsApplePayload(
			ApnsAlert alert,
			int badge
		) {
			Alert = alert;
			Badge = badge;
		}
		public ApnsApplePayload(
			ApnsAlert alert,
			int badge,
			string category
		) {
			Alert = alert;
			Badge = badge;
			Category = category;
		}
		public ApnsAlert Alert { get; }
		public int Badge { get; }
		public string Category { get; }
	}
}