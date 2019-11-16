using api.Notifications;

namespace api.DataAccess.Models {
	public class NotificationPostAlertDispatch : NotificationAlertDispatch {
		public bool HasRecipientReadArticle { get; set; }
	}
}