using System;

namespace api.DataAccess.Models {
	public class NotificationPreference : NotificationPreferenceOptions {
		public long Id { get; set; }
		public long UserAccountId { get; set; }
		public DateTime LastModified { get; set; }
	}
}