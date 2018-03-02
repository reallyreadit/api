using System;

namespace api.Controllers.BulkMailings {
	public class BulkMailingRecipient {
		public Guid UserAccountId { get; set; }
		public bool IsSuccessful { get; set; }
	}
}