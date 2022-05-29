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
using System.Linq;
using System.Security.Claims;
using api.DataAccess;
using Npgsql;

namespace api.Authentication {
   public static class IIdentityExtensions {
		private static Claim GetNameIdentifierClaim(ClaimsPrincipal principal) => (
			principal.Claims.SingleOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)
		);
		private static long GetUserAccountId(Claim idClaim) => (
			Int64.Parse(idClaim.Value)
		);
		public static long GetUserAccountId(this ClaimsPrincipal principal) {
			var idClaim = GetNameIdentifierClaim(principal);
			if (idClaim == null) {
				throw new InvalidOperationException("Principal does not have a NameIdentifier claim.");
			}
			return GetUserAccountId(idClaim);
		}
		public static long? GetUserAccountIdOrDefault(this ClaimsPrincipal principal) {
			var idClaim = GetNameIdentifierClaim(principal);
			if (idClaim == null) {
				return null;
			}
			return GetUserAccountId(idClaim);
		}
	}
}