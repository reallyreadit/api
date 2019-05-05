using System;

namespace api.DataAccess.Models {
	public class KeyMetricsReportRow {
		public DateTime Day { get; set; }
		public int UserAccountsAppCount { get; set; }
		public int UserAccountsBrowserCount { get; set; }
		public int UserAccountsUnknownCount { get; set; }
		public int ReadsAppCount { get; set; }
		public int ReadsBrowserCount { get; set; }
		public int ReadsUnknownCount { get; set; }
		public int CommentsAppCount { get; set; }
		public int CommentsBrowserCount { get; set; }
		public int CommentsUnknownCount { get; set; }
	}
}