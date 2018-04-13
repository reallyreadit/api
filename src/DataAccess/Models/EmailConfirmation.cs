using System;

namespace api.DataAccess.Models {
	public class EmailConfirmation {
		public long Id { get; set; }
		public DateTime DateCreated { get; set; }
		public long UserAccountId { get; set; }
		public string EmailAddress { get; set; }
		public DateTime? DateConfirmed { get; set; }
	}
}