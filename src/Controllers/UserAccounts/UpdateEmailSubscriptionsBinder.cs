namespace api.Controllers.UserAccounts {
	public class UpdateEmailSubscriptionsBinder {
		public string Token { get; set; }
		public bool CommentReplyNotifications { get; set; }
		public bool WebsiteUpdates { get; set; }
		public bool SuggestedReadings { get; set; }
	}
}