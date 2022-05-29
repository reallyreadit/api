// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using api.Analytics;
using api.DataAccess.Models;
using api.Notifications;

namespace api.Controllers.UserAccounts {
	public class UserAccountForm {
		public string Name { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public string CaptchaResponse { get; set; }
		public string TimeZoneName { get; set; }
		public DisplayTheme? Theme { get; set; }
		public SignUpAnalyticsForm Analytics { get; set; }
		public PushDeviceForm PushDevice { get; set; }
	}
}