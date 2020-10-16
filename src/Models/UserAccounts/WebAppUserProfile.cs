using api.DataAccess.Models;

namespace api.Models {
	public class WebAppUserProfile {
		public WebAppUserProfile(
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