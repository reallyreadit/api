using System;

namespace api.DataAccess.Models {
	public class ProvisionalUserAccount {
		public long Id { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateMerged { get; set; }
		public long? MergedUserAccountId { get; set; }
	}
}