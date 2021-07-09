using System;

namespace api.DataAccess.Models {
	public class WeeklyUserActivityReport {
		public DateTime Week { get; set; }
		public int ActiveUserCount { get; set; }
		public int ActiveReaderCount { get; set; }
		public int MinutesReading { get; set; }
		public int MinutesReadingToCompletion { get; set; }
	}
}