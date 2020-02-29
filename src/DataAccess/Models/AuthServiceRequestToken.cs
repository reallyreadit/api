using System;

namespace api.DataAccess.Models {
	public class AuthServiceRequestToken {
		public long Id { get; set; }
		public DateTime DateCreated { get; set; }
		public AuthServiceProvider Provider { get; set; }
		public string TokenValue { get; set; }
		public string TokenSecret { get; set; }
		public DateTime? DateCancelled { get; set; }
		public string SignUpAnalytics { get; set; }
	}
}