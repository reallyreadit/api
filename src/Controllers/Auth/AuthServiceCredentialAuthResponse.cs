using api.DataAccess.Models;
using api.Controllers.Shared;

namespace api.Controllers.Auth {
	public class AuthServiceCredentialAuthResponse {
		public AuthServiceCredentialAuthResponse(
			string authServiceToken
		) {
			AuthServiceToken = authServiceToken;
		}
		public AuthServiceCredentialAuthResponse(
			DisplayPreference displayPreference,
			SubscriptionStatusClientModel subscriptionStatus,
			UserAccount user
		) {
			DisplayPreference = displayPreference;
			SubscriptionStatus = subscriptionStatus;
			User = user;
		}
		public string AuthServiceToken { get; }
		public DisplayPreference DisplayPreference { get; }
		public object SubscriptionStatus { get; }
		public UserAccount User { get; }
	}
}