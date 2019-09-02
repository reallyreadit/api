using api.DataAccess.Stats;
using DbProfile = api.DataAccess.Models.Profile;

namespace api.Controllers.Social {
	public class Profile {
		public Profile(
			DbProfile dbProfile,
			LeaderboardBadge leaderboardBadge
		) {
			UserName = dbProfile.UserName;
			IsFollowed = dbProfile.IsFollowed;
			LeaderboardBadge = leaderboardBadge;
			FolloweeCount = dbProfile.FolloweeCount;
			FollowerCount = dbProfile.FollowerCount;
		}
		public string UserName { get; }
		public bool IsFollowed { get; }
		public LeaderboardBadge LeaderboardBadge { get; }
		public long FolloweeCount { get; }
		public long FollowerCount { get; }
	}
}