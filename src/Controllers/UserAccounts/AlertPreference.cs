namespace api.Controllers.UserAccounts {
	public class AlertPreference {
		public AlertEmailPreference Email { get; set; }
		public bool Extension { get; set; }
		public bool Push { get; set; }
	}
}