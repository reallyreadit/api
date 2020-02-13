namespace api.DataAccess.Models {
	public enum NotificationAuthorizationRequestResult {
		None = 0,
		Granted = 1,
		Denied = 2,
		PreviouslyGranted = 3,
		PreviouslyDenied = 4
	}
}