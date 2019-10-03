using System;

namespace api.DataAccess.Models {
	public class BulkMailing {
		public long Id { get; set; }
		public DateTime DateSent { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public string Type { get; set; }
		public string UserAccount { get; set; }
		public int RecipientCount { get; set; }
	}
}