// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System.Text.Json.Serialization;
using api.Analytics;
using api.Authentication;
using api.Notifications;

namespace api.Controllers.Auth {
	public class AppleIdCredentialAuthForm {
		public string AuthorizationCode { get; set; }
		public string Email { get; set; }
		public string IdentityToken { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public AppleRealUserRating RealUserStatus { get; set; }
		public string User { get; set; }
		public SignUpAnalyticsForm Analytics { get; set; }
		public PushDeviceForm PushDevice { get; set; }
		public AppleClient Client { get; set; }
	}
}