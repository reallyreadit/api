namespace api.Notifications {
	public class ApnsPayload {
		public ApnsPayload(
			ApnsApplePayload applePayload,
			IAlertStatus alertStatus
		) {
			Aps = applePayload;
			AlertStatus = alertStatus;
		}
		public ApnsApplePayload Aps { get; }
		public IAlertStatus AlertStatus { get; }
	}
}