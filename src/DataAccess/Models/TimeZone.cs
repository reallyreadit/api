using System;

namespace api.DataAccess.Models {
	public class TimeZone {
		public long Id { get; set; }
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Territory { get; set; }
		public TimeSpan BaseUtcOffset { get; set; }
	}
}