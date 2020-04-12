using System;

namespace api.DataAccess.Models {
	public class ConversionsReportRow {
		public DateTime Week { get; set; }
		public int VisitCount { get; set; }
		public int SignupCount { get; set; }
		public double SignupConversion { get; set; }
		public int ShareCount { get; set; }
		public double ShareConversion { get; set; }
		public int ArticleViewCount { get; set; }
		public double ArticleViewConversion { get; set; }
		public int ArticleReadCount { get; set; }
		public double ArticleReadConversion { get; set; }
		public int PostTweetCount { get; set; }
		public double PostTweetConversion { get; set; }
	}
}