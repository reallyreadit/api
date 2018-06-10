using System;

namespace api.DataAccess.Models {
	public class UserPage {
		public long Id { get; set; }
		public long PageId { get; set; }
		public long UserAccountId { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime? LastModified { get; set; }
		public int[] ReadState { get; set; }
		public int WordsRead { get; set; }
		public DateTime? DateCompleted { get; set; }
	}
}