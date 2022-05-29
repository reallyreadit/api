// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System;
using api.DataAccess.Models;
using api.DataAccess.Stats;

namespace api.Controllers.Social {
	public class Post {
		public Post(
			DateTime date,
			string userName,
			LeaderboardBadge badge,
			Article article,
			PostComment comment,
			string silentPostId,
			DateTime? dateDeleted,
			bool hasAlert
		) {
			Date = date;
			if (dateDeleted.HasValue) {
				UserName = String.Empty;
				Badge = LeaderboardBadge.None;
			} else {
				UserName = userName;
				Badge = badge;
			}
			Article = article;
			Comment = comment;
			SilentPostId = silentPostId;
			DateDeleted = dateDeleted;
			HasAlert = hasAlert;
		}
		public DateTime Date { get; }
		public string UserName { get; }
		public LeaderboardBadge Badge { get; }
		public Article Article { get; }
		public PostComment Comment { get; }
		public string SilentPostId { get; set; }
		public DateTime? DateDeleted { get; set; }
		public bool HasAlert { get; set; }
	}
}