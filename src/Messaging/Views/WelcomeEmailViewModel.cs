using System;

namespace api.Messaging.Views {
	public class WelcomeEmailViewModel {
		public WelcomeEmailViewModel(
			Uri profileUrl,
			Uri readManifestoUrl,
			string userName
		) {
			ProfileUrl = profileUrl.ToString();
			ReadManifestoUrl = readManifestoUrl.ToString();
			UserName = userName;
		}
		public string ProfileUrl { get; }
		public string ReadManifestoUrl { get; }
		public string UserName { get; }
	}
}