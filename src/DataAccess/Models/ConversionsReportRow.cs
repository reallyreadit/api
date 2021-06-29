using System;

namespace api.DataAccess.Models {
	public class ConversionsReportRow {
		public DateTime Week { get; set; }
		public int VisitorCount { get; set; }
		public int SignupCount { get; set; }
		public double SignupConversion { get; set; }
		public int ArticleViewerCount { get; set; }
		public double ArticleViewerConversion { get; set; }
		public int ArticleReaderCount { get; set; }
		public double ArticleReaderConversion { get; set; }
	}
}