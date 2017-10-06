using api.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;

namespace api.Authorization {
	public class AuthorizeUserAccountRoleAttribute : AuthorizeAttribute {
		public AuthorizeUserAccountRoleAttribute() {}
		public AuthorizeUserAccountRoleAttribute(UserAccountRole role) {
			Roles = role.ToString();
		}
	}
}