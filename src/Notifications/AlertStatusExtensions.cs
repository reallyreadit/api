namespace api.Notifications {
	public static class AlertStatusExtensions {
		public static int GetTotalBadgeCount(this IAlertStatus status) => (
			(status.AotdAlert ? 1 : 0) +
			status.FollowerAlertCount +
			status.ReplyAlertCount
		);
	}
}