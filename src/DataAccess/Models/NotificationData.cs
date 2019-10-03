namespace api.DataAccess.Models {
	public class NotificationData { 
		public long Id { get; set; }
		public long EventId { get; set; }
		public long? ArticleId { get; set; }
		public long? CommentId { get; set; }
		public long? SilentPostId { get; set; }
		public long? FollowingId { get; set; }
	}
}