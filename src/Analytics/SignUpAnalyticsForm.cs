// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

namespace api.Analytics {
	public class SignUpAnalyticsForm {
		public string Action { get; set; }
		public string CurrentPath { get; set; }
		public string InitialPath { get; set; }
		public int MarketingVariant { get; set; }
		public string ReferrerUrl { get; set; }
	}
}