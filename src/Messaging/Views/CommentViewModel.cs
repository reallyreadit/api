using System;

namespace api.Messaging.Views {
	public class CommentViewModel {
		public CommentViewModel(
			string author,
			string article,
			string text,
			Uri readArticleUrl,
			Uri viewCommentUrl
		) {
			Author = author;
			Article = article;
			Text = text;
			ReadArticleUrl = readArticleUrl.ToString();
			ViewCommentUrl = viewCommentUrl.ToString();
		}
		public string Author { get; }
		public string Article { get; }
		public string Text { get; }
		public string ReadArticleUrl { get; }
		public string ViewCommentUrl { get; }
	}
}