using System;

namespace api.Messaging.Views {
	public class FreeTrialCompletionEmailViewModel {
		public FreeTrialCompletionEmailViewModel(
			string userName,
			Uri subscribeUrl,
			Uri writerLeaderboardUrl
		) {
			UserName = userName;
			SubscribeUrl = subscribeUrl.ToString();
			WriterLeaderboardUrl = writerLeaderboardUrl.ToString();
		}
		public string UserName { get; }
		public string SubscribeUrl { get; }
		public string WriterLeaderboardUrl { get; }
	}
}