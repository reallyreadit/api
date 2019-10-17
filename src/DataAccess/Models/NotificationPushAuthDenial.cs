using System;

namespace api.DataAccess.Models {
	public class NotificationPushAuthDenial {
		public long Id { get; set; }
		public DateTime DateDenied { get; set; }
		public long UserAccountId { get; set; }
		public string InstallationId { get; set; }
		public string DeviceName { get; set; }
	}
}