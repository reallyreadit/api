using System;
using System.Linq;
using System.Security.Claims;
using api.DataAccess;
using Npgsql;

namespace api.Authentication {
   public static class IIdentityExtensions {
		private static Claim GetNameIdentifierClaim(ClaimsPrincipal principal) {
			return principal.Claims.SingleOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
		}
		private static long GetUserAccountId(Claim idClaim, NpgsqlConnection db) {
			long id;
			if (Int64.TryParse(idClaim.Value, out id)) {
				return id;
			}
			return db.GetUserAccountUsingOldId(Guid.Parse(idClaim.Value)).Id;
		}
		public static long GetUserAccountId(this ClaimsPrincipal principal, NpgsqlConnection db) {
			var idClaim = GetNameIdentifierClaim(principal);
			if (idClaim == null) {
				throw new InvalidOperationException("Principal does not have a NameIdentifier claim.");
			}
			return GetUserAccountId(idClaim, db);
		}
		public static long? GetUserAccountIdOrDefault(this ClaimsPrincipal principal, NpgsqlConnection db) {
			var idClaim = GetNameIdentifierClaim(principal);
			if (idClaim == null) {
				return null;
			}
			return GetUserAccountId(idClaim, db);
		}
	}
}