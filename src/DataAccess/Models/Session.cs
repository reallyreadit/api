using System;

namespace api.DataAccess.Models {
	public class Session {
		public byte[] Id { get; set; }
		public Guid UserAccountId { get; set; }
		public DateTime BeginDate { get; set; }
		public DateTime? EndDate { get; set; }
	}
}