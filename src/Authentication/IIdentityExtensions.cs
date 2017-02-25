using System;
using System.Linq;
using System.Security.Claims;

namespace api.Authentication {
   public static class IIdentityExtensions {
		private static Claim GetNameIdentifierClaim(ClaimsPrincipal principal) {
			return principal.Claims.SingleOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
		}
		public static Guid GetUserAccountId(this ClaimsPrincipal principal) {
			var idClaim = GetNameIdentifierClaim(principal);
			if (idClaim == null) {
				throw new InvalidOperationException("Principal does not have a NameIdentifier claim.");
			}
			return Guid.Parse(idClaim.Value);
		}
		public static Guid? GetUserAccountIdOrDefault(this ClaimsPrincipal principal) {
			var idClaim = GetNameIdentifierClaim(principal);
			return idClaim != null ? new Nullable<Guid>(Guid.Parse(idClaim.Value)) : null;
		}
	}
}