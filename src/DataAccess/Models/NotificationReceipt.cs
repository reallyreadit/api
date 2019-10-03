using System;

namespace api.DataAccess.Models {
	public class NotificationReceipt { 
		public long Id { get; set; }
		public long EventId { get; set; }
		public long UserAccountId { get; set; }
		public DateTime? DateAlertCleared { get; set; }
		public bool ViaEmail { get; set; }
		public bool ViaExtension { get; set; }
		public bool ViaPush { get; set; }
	}
}