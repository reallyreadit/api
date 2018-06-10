using System;

namespace api.DataAccess.Models {
	public class Challenge {
		public long Id { get; set; }
		public string Name { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public int AwardLimit { get; set; }
		public int AwardCount { get; set; }
	}
}