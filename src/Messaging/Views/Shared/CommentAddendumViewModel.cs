using api.Commenting;
using api.DataAccess.Models;

namespace api.Messaging.Views.Shared {
	public class CommentAddendumViewModel {
		public CommentAddendumViewModel(
			CommentAddendum addendum
		) {
			DateCreated = addendum.DateCreated.ToShortDateString();
			TextHtml = CommentingService.RenderCommentTextToHtml(addendum.TextContent);
		}
		public string DateCreated { get; }
		public string TextHtml { get; }
	}
}