using System;

namespace api.DataAccess.Models {
	public class NotificationDigestFollower {
		public NotificationDigestFollower(
			long followingId,
			DateTime dateFollowed,
			string userName
		) {
			FollowingId = followingId;
			DateFollowed = dateFollowed;
			UserName = userName;
		}
		public long FollowingId { get; }
		public DateTime DateFollowed { get; }
		public string UserName { get; }
	}
}