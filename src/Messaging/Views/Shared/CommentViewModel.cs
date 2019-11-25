using System;
using System.Collections.Generic;
using api.DataAccess.Models;

namespace api.Messaging.Views.Shared {
	public class CommentViewModel {
		public CommentViewModel(
			string author,
			string article,
			string text,
			IEnumerable<CommentAddendum> addenda,
			Uri readArticleUrl,
			Uri viewCommentUrl
		) {
			Author = author;
			Article = article;
			CommentText = new CommentTextViewModel(text, addenda);
			ReadArticleUrl = readArticleUrl.ToString();
			ViewCommentUrl = viewCommentUrl.ToString();
		}
		public string Author { get; }
		public string Article { get; }
		public CommentTextViewModel CommentText { get; }
		public string ReadArticleUrl { get; }
		public string ViewCommentUrl { get; }
	}
}