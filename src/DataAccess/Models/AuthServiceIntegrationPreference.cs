using System;

namespace api.DataAccess.Models {
	public class AuthServiceIntegrationPreference {
		public long Id { get; set; }
		public DateTime LastModified { get; set; }
		public long IdentityId { get; set; }
		public bool IsPostEnabled { get; set; }
	}
}