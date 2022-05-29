// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using api.DataAccess.Stats;

namespace api.DataAccess.Models {
	public class UserLeaderboardRankings {
		public Ranking LongestRead { get; set; }
		public Ranking ReadCount { get; set; }
		public Ranking ScoutCount { get; set; }
		public Ranking ScribeCount { get; set; }
		public StreakRanking Streak { get; set; }
		public Ranking WeeklyReadCount { get; set; }
		public LeaderboardBadge GetBadge() {
			var badge = LeaderboardBadge.None;
			foreach (
				 var kvp in new Dictionary<LeaderboardBadge, IRanking>() {
					{ LeaderboardBadge.ReadCount, ReadCount },
					{ LeaderboardBadge.Scout, ScoutCount },
					{ LeaderboardBadge.Scribe, ScribeCount },
					{ LeaderboardBadge.Streak, Streak },
					{ LeaderboardBadge.WeeklyReadCount, WeeklyReadCount }
				}
			) {
				if (kvp.Value.Rank != 0 && kvp.Value.Rank <= Leaderboards.MaxRank) {
					badge |= kvp.Key;
				}
			}
			return badge;
		}
	}
}