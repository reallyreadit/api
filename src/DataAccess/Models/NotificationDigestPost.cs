using System;

namespace api.DataAccess.Models {
	public class NotificationDigestPost {
		public NotificationDigestPost(
			long? commentId,
			long? silentPostId,
			DateTime dateCreated,
			string commentText,
			CommentAddendum[] commentAddenda,
			string author,
			long articleId,
			string articleTitle
		) {
			CommentId = commentId;
			SilentPostId = silentPostId;
			DateCreated = dateCreated;
			CommentText = commentText;
			CommentAddenda = commentAddenda;
			Author = author;
			ArticleId = articleId;
			ArticleTitle = articleTitle;
		}
		public long? CommentId { get; }
		public long? SilentPostId { get; }
		public DateTime DateCreated { get; }
		public string CommentText { get; }
		public CommentAddendum[] CommentAddenda { get; }
		public string Author { get; }
		public long ArticleId { get; }
		public string ArticleTitle { get; }
	}
}