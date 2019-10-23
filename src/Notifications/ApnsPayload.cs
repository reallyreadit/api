using System;

namespace api.Notifications {
	public class ApnsPayload {
		public ApnsPayload(
			ApnsApplePayload applePayload,
			IAlertStatus alertStatus,
			Uri url
		) {
			Aps = applePayload;
			AlertStatus = alertStatus;
			Url = url.ToString();
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
		public string Url { get; }
	}
}