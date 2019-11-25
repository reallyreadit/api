using System;
using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.Messaging.Views.Shared {
	public class CommentTextViewModel {
		public CommentTextViewModel(
			string text,
			IEnumerable<CommentAddendum> addenda
		) {
			Text = text;
			Addenda = addenda
				.OrderBy(
					addendum => addendum.DateCreated
				)
				.Select(
					addendum => new CommentAddendumViewModel(addendum)
				)
				.ToArray();
		}
		public string Text { get; }
		public CommentAddendumViewModel[] Addenda { get; }
	}
}