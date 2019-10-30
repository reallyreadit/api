namespace api.Notifications {
	public interface INotificationDispatch {
		long ReceiptId { get; }
		long UserAccountId { get; }
		string UserName { get; }
		string EmailAddress { get; }
	}
}