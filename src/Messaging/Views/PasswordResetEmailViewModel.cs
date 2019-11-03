using System;

namespace api.Messaging.Views {
	public class PasswordResetEmailViewModel {
		public PasswordResetEmailViewModel(
			Uri passwordResetUrl
		) {
			PasswordResetUrl = passwordResetUrl.ToString();
		}
		public string PasswordResetUrl { get; }
	}
}