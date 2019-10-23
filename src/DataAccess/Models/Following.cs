using System;

namespace api.DataAccess.Models {
	public class Following {
		public long Id { get; set; }
		public long FollowerUserAccountId { get; set; }
		public long FolloweeUserAccountId { get; set; }
		public DateTime DateFollowed { get; set; }
		public DateTime? DateUnfollowed { get; set; }
	}
}