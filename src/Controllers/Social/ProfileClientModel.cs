// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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