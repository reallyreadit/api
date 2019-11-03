using System;

namespace api.DataAccess.Models {
	public class NotificationFollowerDigestDispatch {
		public long ReceiptId { get; set; }
		public long UserAccountId { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
		public long FollowerFollowingId { get; set; }
		public DateTime FollowerDateFollowed { get; set; }
		public string FollowerUserName { get; set; }
	}
}