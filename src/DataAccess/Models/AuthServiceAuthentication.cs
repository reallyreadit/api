using System;

namespace api.DataAccess.Models {
	public class AuthServiceAuthentication {
		public long Id { get; set; }
		public DateTime DateAuthenticated { get; set; }
		public long IdentityId { get; set; }
		public string SessionId { get; set; }
	}
}