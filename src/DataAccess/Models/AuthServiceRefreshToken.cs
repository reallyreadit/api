using System;

namespace api.DataAccess.Models {
	public class AuthServiceRefreshToken {
		public DateTime DateCreated { get; set; }
		public long IdentityId { get; set; }
		public string RawValue { get; set; }
	}
}