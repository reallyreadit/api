using api.DataAccess.Models;

namespace api.Messaging.Views.Shared {
	public class CommentAddendumViewModel {
		public CommentAddendumViewModel(
			CommentAddendum addendum
		) {
			DateCreated = addendum.DateCreated.ToShortDateString();
			Text = addendum.TextContent;
		}
		public string DateCreated { get; }
		public string Text { get; }
	}
}