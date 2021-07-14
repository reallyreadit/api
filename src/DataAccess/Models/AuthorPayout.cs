using System;

namespace api.DataAccess.Models {
	public class AuthorPayout {
		public string Id { get; set; }
		public DateTime DateCreated { get; set; }
		public string PayoutAccountId { get; set; }
		public int Amount { get; set; }
	}
}