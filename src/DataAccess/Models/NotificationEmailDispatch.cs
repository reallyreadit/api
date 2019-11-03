using api.Notifications;

public class NotificationEmailDispatch : INotificationDispatch {
	public long ReceiptId { get; set; }
	public long UserAccountId { get; set; }
	public string UserName { get; set; }
	public string EmailAddress { get; set; }
}