using api.DataAccess.Stats;
using api.DataAccess.Models;
using api.Controllers.Shared;

namespace api.Controllers.Social {
	public class ProfileClientModel {
		public ProfileClientModel(
			Profile profile,
			LeaderboardBadge leaderboardBadge,
			AuthorProfileClientModel authorProfile
		) {
			UserName = profile.UserName;
			IsFollowed = profile.IsFollowed;
			LeaderboardBadge = leaderboardBadge;
			FolloweeCount = profile.FolloweeCount;
			FollowerCount = profile.FollowerCount;
			AuthorProfile = authorProfile;
		}
		public string UserName { get; }
		public bool IsFollowed { get; }
		public LeaderboardBadge LeaderboardBadge { get; }
		public long FolloweeCount { get; }
		public long FollowerCount { get; }
		public AuthorProfileClientModel AuthorProfile { get; }
	}
}