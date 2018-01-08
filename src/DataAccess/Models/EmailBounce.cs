using System;

namespace api.DataAccess.Models {
	public class EmailBounce {
		public Guid Id { get; set; }
		public DateTime DateReceived { get; set; }
		public string Address { get; set; }
		public string Message { get; set; }
		public Guid? BulkMailingId { get; set; }
	}
}