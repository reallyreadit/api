using System;

namespace api.Messaging.Views.Shared {
	public class FollowerViewModel {
		public FollowerViewModel(
			string userName,
			Uri viewProfileUrl
		) {
			UserName = userName;
			ViewProfileUrl = viewProfileUrl.ToString();
		}
		public string UserName { get; }
		public string ViewProfileUrl { get; }
	}
}