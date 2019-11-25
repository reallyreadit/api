using System;

namespace api.DataAccess.Models {
	public class NotificationDigestComment {
		public NotificationDigestComment(
			long id,
			DateTime dateCreated,
			string text,
			CommentAddendum[] addenda,
			string author,
			long articleId,
			string articleTitle
		) {
			Id = id;
			DateCreated = dateCreated;
			Text = text;
			Addenda = addenda;
			Author = author;
			ArticleId = articleId;
			ArticleTitle = articleTitle;
		}
		public long Id { get; }
		public DateTime DateCreated { get; }
		public string Text { get; }
		public CommentAddendum[] Addenda { get; }
		public string Author { get; }
		public long ArticleId { get; }
		public string ArticleTitle { get; }
	}
}