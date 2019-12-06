using System;
using System.Collections.Generic;
using System.Linq;
using api.Commenting;
using api.DataAccess.Models;
using api.Markdown;
using Markdig;

namespace api.Messaging.Views.Shared {
	public class CommentTextViewModel {
		public CommentTextViewModel(
			string text,
			IEnumerable<CommentAddendum> addenda
		) {
			TextHtml = CommentingService.RenderCommentTextToHtml(text);
			if (addenda != null) {
				Addenda = addenda
					.OrderBy(
						addendum => addendum.DateCreated
					)
					.Select(
						addendum => new CommentAddendumViewModel(addendum)
					)
					.ToArray();
			} else {
				Addenda = new CommentAddendumViewModel[0];
			}
		}
		public string TextHtml { get; }
		public CommentAddendumViewModel[] Addenda { get; }
	}
}