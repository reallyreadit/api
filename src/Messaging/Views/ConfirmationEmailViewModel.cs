using System;

namespace api.Messaging.Views {
	public class ConfirmationEmailViewModel {
		public ConfirmationEmailViewModel(
			Uri emailConfirmationUrl
		) {
			EmailConfirmationUrl = emailConfirmationUrl.ToString();
		}
		public string EmailConfirmationUrl { get; }
	}
}