using api.DataAccess.Models;

namespace api.Controllers.Notifications {
	public class DigestForm {
		public string ApiKey { get; set; }
		public NotificationEventFrequency Frequency { get; set; }
	}
}