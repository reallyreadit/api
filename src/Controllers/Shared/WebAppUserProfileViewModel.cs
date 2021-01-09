using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public class WebAppUserProfileViewModel {
		public WebAppUserProfileViewModel(
			DisplayPreference displayPreference,
			UserAccount userAccount
		) {
			DisplayPreference = displayPreference;
			UserAccount = userAccount;
		}
		public DisplayPreference DisplayPreference { get; }
		public UserAccount UserAccount { get; }
	}
}