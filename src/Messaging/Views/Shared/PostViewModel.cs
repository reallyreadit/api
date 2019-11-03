using System;

namespace api.Messaging.Views.Shared {
	public class PostViewModel {
		public PostViewModel(
			string author,
			string article,
			string text,
			Uri readArticleUrl,
			Uri viewPostUrl
		) {
			Author = author;
			Article = article;
			Text = text;
			ReadArticleUrl = readArticleUrl.ToString();
			ViewPostUrl = viewPostUrl.ToString();
		}
		public string Author { get; }
		public string Article { get; }
		public string Text { get; }
		public string ReadArticleUrl { get; }
		public string ViewPostUrl { get; }
	}
}