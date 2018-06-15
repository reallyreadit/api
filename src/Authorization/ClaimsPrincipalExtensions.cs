using System.Security.Claims;
using api.DataAccess.Models;

namespace api.Authorization {
	public static class ClaimsPrincipalExtensions {
		public static bool IsInRole(this ClaimsPrincipal principal, UserAccountRole role) => (
			principal.IsInRole(role.ToString())
		);
	}
}