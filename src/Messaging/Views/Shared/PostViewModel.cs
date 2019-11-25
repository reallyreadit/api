using System;
using System.Collections.Generic;
using api.DataAccess.Models;

namespace api.Messaging.Views.Shared {
	public class PostViewModel {
		public PostViewModel(
			string author,
			string article,
			string text,
			IEnumerable<CommentAddendum> addenda,
			Uri readArticleUrl,
			Uri viewPostUrl
		) {
			Author = author;
			Article = article;
			CommentText = new CommentTextViewModel(text, addenda);
			ReadArticleUrl = readArticleUrl.ToString();
			ViewPostUrl = viewPostUrl.ToString();
		}
		public string Author { get; }
		public string Article { get; }
		public CommentTextViewModel CommentText { get; }
		public string ReadArticleUrl { get; }
		public string ViewPostUrl { get; }
	}
}