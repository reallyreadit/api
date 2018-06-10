namespace api.Controllers.UserAccounts {
	public class PasswordResetRequestBinder {
		public string Email { get; set; }
		public string CaptchaResponse { get; set; }
	}
}