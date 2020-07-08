namespace api.Controllers.Notifications {
	public class ClearAlertForm {
		// deprecated legacy parameter
		public int Alert { get; set; }
		public Alert Alerts { get; set; }
	}
}