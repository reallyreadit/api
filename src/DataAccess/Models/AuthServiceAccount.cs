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
	public class AuthServiceAccount {
		public long IdentityId { get; set; }
		public DateTime DateIdentityCreated { get; set; }
		public string IdentitySignUpAnalytics { get; set; }
		public AuthServiceProvider Provider { get; set; }
		public string ProviderUserId { get; set; }
		public string ProviderUserEmailAddress { get; set; }
		public bool IsEmailAddressPrivate { get; set; }
		public string ProviderUserName { get; set; }
		public string ProviderUserHandle { get; set; }
		public DateTime? DateUserAccountAssociated { get; set; }
		public long? AssociatedUserAccountId { get; set; }
		public string AccessTokenValue { get; set; }
		public string AccessTokenSecret { get; set; }
	}
}