using System;

namespace api.DataAccess.Models {
	public class CreateBulkMailingRecipient {
		public Guid UserAccountId { get; set; }
		public bool IsSuccessful { get; set; }
	}
}