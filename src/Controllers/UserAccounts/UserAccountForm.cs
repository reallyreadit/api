using api.Analytics;
using api.DataAccess.Models;
using api.Notifications;

namespace api.Controllers.UserAccounts {
	public class UserAccountForm {
		public string Name { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public string CaptchaResponse { get; set; }
		public string TimeZoneName { get; set; }
		public DisplayTheme? Theme { get; set; }
		public SignUpAnalyticsForm Analytics { get; set; }
		public PushDeviceForm PushDevice { get; set; }
	}
}