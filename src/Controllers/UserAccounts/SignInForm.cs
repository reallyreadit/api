namespace api.Controllers.UserAccounts {
	public class SignInForm {
		public string Email { get; set; }
		public string Password { get; set; }
		public PushDeviceForm PushDevice { get; set; }
	}
}