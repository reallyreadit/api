using System;

namespace api.DataAccess.Models {
	public class PasswordResetRequest {
		public Guid Id { get; set; }
		public DateTime DateCreated { get; set; }
		public Guid UserAccountId { get; set; }
		public string EmailAddress { get; set; }
		public DateTime? DateCompleted { get; set; }
	}
}