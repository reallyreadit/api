using api.Notifications;

namespace api.DataAccess.Models {
	public class NotificationAlertDispatch : IAlertStatus, INotificationDispatch {
		public long ReceiptId { get; set; }
		public bool ViaEmail { get; set; }
		public bool ViaPush { get; set; }
		public long UserAccountId { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
		public string[] PushDeviceTokens { get; set; }
		public bool AotdAlert { get; set; }
		public int ReplyAlertCount { get; set; }
		public int LoopbackAlertCount { get; set; }
		public int PostAlertCount { get; set; }
		public int FollowerAlertCount { get; set; }
	}
}