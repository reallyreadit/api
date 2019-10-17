namespace api.Controllers.UserAccounts {
	public class UserAccountForm {
		public string Name { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public string CaptchaResponse { get; set; }
		public string TimeZoneName { get; set; }
		public int MarketingScreenVariant { get; set; }
		public string ReferrerUrl { get; set; }
		public string InitialPath { get; set; }
		public PushDeviceForm PushDevice { get; set; }
	}
}