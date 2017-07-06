namespace api.Controllers.UserAccounts {
	public class ChangePasswordBinder {
		public string CurrentPassword { get; set; }
		public string NewPassword { get; set; }
	}
}