namespace api.Controllers.UserAccounts {
	public class PasswordResetForm {
		public string Token { get; set; }
		public string Password { get; set; }
		public PushDeviceForm PushDevice { get; set; }
	}
}