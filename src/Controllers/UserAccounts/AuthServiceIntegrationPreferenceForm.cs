using api.Authentication;

namespace api.Controllers.UserAccounts {
	public class AuthServiceIntegrationPreferenceForm {
		public long IdentityId { get; set; }
		public AuthServiceIntegration Integration { get; set; }
	}
}