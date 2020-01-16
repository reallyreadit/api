using api.Notifications;

namespace api.Controllers.UserAccounts {
	public class AuthServiceAccountForm {
		public string Token { get; set; }
		public string Name { get; set; }
		public string TimeZoneName { get; set; }
		public PushDeviceForm PushDevice { get; set; }
	}
}