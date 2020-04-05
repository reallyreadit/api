using System;
using System.Text.RegularExpressions;

namespace api.Messaging.Views {
	public class WelcomeEmailViewModel : ConfirmationEmailViewModel {
		public WelcomeEmailViewModel(
			Uri profileUrl,
			Uri emailConfirmationUrl
		) : base(
			emailConfirmationUrl
		) {
			ProfileUrl = profileUrl.ToString();
			ProfileLinkText = Regex.Replace(
				input: profileUrl.ToString(),
				pattern: "^https?://",
				replacement: String.Empty
			);
		}
		public string ProfileUrl { get; }
		public string ProfileLinkText { get; }
	}
}