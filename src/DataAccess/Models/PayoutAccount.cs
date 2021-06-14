using System;

namespace api.DataAccess.Models {
	public class PayoutAccount {
		public string Id { get; set; }
		public long UserAccountId { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime? DateDetailsSubmitted { get; set; }
		public DateTime? DatePayoutsEnabled { get; set; }
	}
}