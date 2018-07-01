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