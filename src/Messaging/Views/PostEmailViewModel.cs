using api.Messaging.Views.Shared;

namespace api.Messaging.Views {
	public class PostEmailViewModel {
		public PostEmailViewModel(
			PostViewModel post,
			bool isReplyable
		) {
			Post = post;
			IsReplyable = isReplyable;
		}
		public PostViewModel Post { get; }
		public bool IsReplyable { get; }
	}
}