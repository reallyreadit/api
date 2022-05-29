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

namespace api.DataAccess.Models {
	public class DailyTotalsReportRow {
		public DateTime Day { get; set; }
		public int SignupAppCount { get; set; }
		public int SignupBrowserCount { get; set; }
		public int SignupUnknownCount { get; set; }
		public int ReadAppCount { get; set; }
		public int ReadBrowserCount { get; set; }
		public int ReadUnknownCount { get; set; }
		public int PostAppCount { get; set; }
		public int PostBrowserCount { get; set; }
		public int PostUnknownCount { get; set; }
		public int ReplyAppCount { get; set; }
		public int ReplyBrowserCount { get; set; }
		public int ReplyUnknownCount { get; set; }
		public int PostTweetAppCount { get; set; }
		public int PostTweetBrowserCount { get; set; }
		public int ExtensionInstallationCount { get; set; }
		public int ExtensionRemovalCount { get; set; }
		public int SubscriptionsActiveCount { get; set; }
		public int SubscriptionLapseCount { get; set; }
	}
}