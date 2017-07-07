namespace api.Controllers.UserAccounts {
	public class ResetPasswordBinder {
		public string Token { get; set; }
		public string Password { get; set; }
	}
}