using System;

namespace api.DataAccess.Models {
	public class ReadingTimeTotalsRow {
		public DateTime Date { get; set; }
		public int MinutesReading { get; set; }
		public int MinutesReadingToCompletion { get; set; }
	}
}