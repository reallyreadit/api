using System;

namespace api.DataAccess.Models {
	public class DonationRecipient {
		public long Id { get; set; }
		public DateTime DateCreated { get; set; }
		public string Name { get; set; }
		public string Website { get; set; }
		public string TaxId { get; set; }
	}
}