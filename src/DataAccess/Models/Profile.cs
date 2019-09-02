namespace api.DataAccess.Models {
	public class Profile {
		public string UserName { get; set; }
		public bool IsFollowed { get; set; }
		public long FolloweeCount { get; set; }
		public long FollowerCount { get; set; }
	}
}