namespace api.Notifications {
	public interface IAlertStatus {
		bool AotdAlert { get; }
		int ReplyAlertCount { get; }
		int LoopbackAlertCount { get; }
		int PostAlertCount { get; }
		int FollowerAlertCount { get; }
	}
}