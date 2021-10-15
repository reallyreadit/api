using System;

namespace api.Messaging.Views {
	public class WelcomeEmailViewModel {
		public WelcomeEmailViewModel(
			Uri downloadUrl,
			Uri profileUrl,
			string userName
		) {
			DownloadUrl = downloadUrl.ToString();
			ProfileUrl = profileUrl.ToString();
			UserName = userName;
		}
		public string DownloadUrl { get; }
		public string ProfileUrl { get; }
		public string UserName { get; }
	}
}