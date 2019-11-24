using System;

namespace api.DataAccess.Models {
	public class NotificationCommentDigestDispatch {
		public long ReceiptId { get; set; }
		public long UserAccountId { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
		public long CommentId { get; set; }
		public DateTime CommentDateCreated { get; set; }
		public string CommentText { get; set; }
		public CommentAddendum[] CommentAddenda { get; set; }
		public string CommentAuthor { get; set; }
		public long CommentArticleId { get; set; }
		public string CommentArticleTitle { get; set; }
	}
}