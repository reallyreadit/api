using api.Notifications;

namespace api.Controllers.UserAccounts {
	public class SignInForm {
		public string AuthServiceToken { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public PushDeviceForm PushDevice { get; set; }
	}
}