namespace api.Controllers.UserAccounts {
	public class PasswordResetRequestForm {
		public string AuthServiceToken { get; set; }
		public string Email { get; set; }
		public string CaptchaResponse { get; set; }
	}
}