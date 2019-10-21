namespace api.Notifications {
	public class ApnsPayload {
		public ApnsPayload(
			ApnsApplePayload applePayload,
			IAlertStatus alertStatus
		) {
			Aps = applePayload;
			AlertStatus = alertStatus;
		}
		public ApnsPayload(
			ApnsApplePayload applePayload,
			IAlertStatus alertStatus,
			string[] clearedNotificationIds
		) {
			Aps = applePayload;
			AlertStatus = alertStatus;
			ClearedNotificationIds = clearedNotificationIds;
		}
		public ApnsApplePayload Aps { get; }
		public IAlertStatus AlertStatus { get; }
		public string[] ClearedNotificationIds { get; }
	}
}