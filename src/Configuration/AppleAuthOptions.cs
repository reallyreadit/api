// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

namespace api.Configuration {
	public class AppleAuthOptions {
		public string AppleJwkUrl { get; set; }
		public string ClientSecretAudience { get; set; }
		public string ClientSecretSigningKeyId { get; set; }
		public string ClientSecretSigningKeyPath { get; set; }
		public string DeveloperAppId { get; set; }
		public string DeveloperTeamId { get; set; }
		public string DeveloperWebServiceId { get; set; }
		public string IdTokenIssuer { get; set; }
		public string IdTokenValidationUrl { get; set; }
		public string WebAuthPopupRedirectUrl { get; set; }
		public string WebAuthRedirectUrl { get; set; }
		public string WebAuthUrl { get; set; }
	}
}