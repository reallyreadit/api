using System;

namespace api.DataAccess.Models {
	public class NotificationPostDigestDispatch {
		public long ReceiptId { get; set; }
		public long UserAccountId { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
		public long? PostCommentId { get; set; }
		public long? PostSilentPostId { get; set; }
		public DateTime PostDateCreated { get; set; }
		public string PostCommentText { get; set; }
		public CommentAddendum[] PostCommentAddenda { get; set; }
		public string PostAuthor { get; set; }
		public long PostArticleId { get; set; }
		public string PostArticleTitle { get; set; }
	}
}