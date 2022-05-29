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
using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.DataAccess.Stats {
	public class Leaderboards {
		public static TimeSpan LongestReadOffset => TimeSpan.FromDays(30);
		public static TimeSpan ScoutOffset => TimeSpan.FromDays(30);
		public static TimeSpan ScribeOffset => TimeSpan.FromDays(30);
		public static int MaxRank => 5;
		public IEnumerable<LeaderboardRanking> LongestRead { get; set; }
		public IEnumerable<LeaderboardRanking> ReadCount { get; set; }
		public IEnumerable<LeaderboardRanking> Scout { get; set; }
		public IEnumerable<LeaderboardRanking> Scribe { get; set; }
		public IEnumerable<LeaderboardRanking> Streak { get; set; }
		public IEnumerable<LeaderboardRanking> WeeklyReadCount { get; set; }
		public LeaderboardBadge GetBadge(
			string userName
		) {
			var badge = LeaderboardBadge.None;
			foreach (
				var kvp in new Dictionary<LeaderboardBadge, IEnumerable<LeaderboardRanking>>() {
					{ LeaderboardBadge.ReadCount, ReadCount },
					{ LeaderboardBadge.Scout, Scout },
					{ LeaderboardBadge.Scribe, Scribe },
					{ LeaderboardBadge.Streak, Streak },
					{ LeaderboardBadge.WeeklyReadCount, WeeklyReadCount }
				}
			) {
				foreach (var ranking in kvp.Value.Where(ranking => ranking.UserName == userName)) {
					badge |= kvp.Key;
				}
			}
			return badge;
		}
	}
}