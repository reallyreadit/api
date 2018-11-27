namespace api.Controllers.UserAccounts {
	public class CreateAccountBinder {
		public string Name { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public string CaptchaResponse { get; set; }
		public string TimeZoneName { get; set; }
	}
}