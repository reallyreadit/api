using System;
using api.DataAccess.Models;

namespace api.Controllers.Analytics {
	public class OrientationForm {
		public int TrackingPlayCount { get; set; }
		public bool TrackingSkipped { get; set; }
		public int TrackingDuration { get; set; }
		public int ImportPlayCount { get; set; }
		public bool ImportSkipped { get; set; }
		public int ImportDuration { get; set; }
		public NotificationAuthorizationRequestResult NotificationsResult { get; set; }
		public bool NotificationsSkipped { get; set; }
		public int NotificationsDuration { get; set; }
		public Guid? ShareResultId { get; set; }
		public bool ShareSkipped { get; set; }
		public int ShareDuration { get; set; }
	}
}