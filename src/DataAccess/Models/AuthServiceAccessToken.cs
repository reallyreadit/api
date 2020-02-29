using System;

namespace api.DataAccess.Models {
	public class AuthServiceAccessToken {
		public DateTime DateCreated { get; set; }
		public DateTime LastStored { get; set; }
		public long IdentityId { get; set; }
		public long RequestId { get; set; }
		public string TokenValue { get; set; }
		public string TokenSecret { get; set; }
		public DateTime? DateRevoked { get; set; }
	}
}