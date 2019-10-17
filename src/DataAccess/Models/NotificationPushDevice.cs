using System;

namespace api.DataAccess.Models {
	public class NotificationPushDevice {
		public long Id { get; set; }
		public DateTime DateRegistered { get; set; }
		public DateTime? DateUnregistered { get; set; }
		public NotificationPushUnregistrationReason UnregistrationReason { get; set; }
		public long UserAccountId { get; set; }
		public string InstallationId { get; set; }
		public string Name { get; set; }
		public string Token { get; set; }
	}
}