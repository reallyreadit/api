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
		public ApnsAlert Alert { get; }
		public int Badge { get; }		
	}
}