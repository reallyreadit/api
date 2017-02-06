using System;

namespace api.DataAccess.Models {
	public class UserPage {
		public Guid Id { get; set; }
		public Guid PageId { get; set; }
		public Guid UserAccountId { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime? LastModified { get; set; }
		public int[] ReadState { get; set; }
		public int WordsRead { get; set; }
	}
}