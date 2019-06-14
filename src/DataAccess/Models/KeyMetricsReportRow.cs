using System;

namespace api.DataAccess.Models {
	public class KeyMetricsReportRow {
		public DateTime Day { get; set; }
		public int UserAccountAppCount { get; set; }
		public int UserAccountBrowserCount { get; set; }
		public int UserAccountUnknownCount { get; set; }
		public int ReadAppCount { get; set; }
		public int ReadBrowserCount { get; set; }
		public int ReadUnknownCount { get; set; }
		public int CommentAppCount { get; set; }
		public int CommentBrowserCount { get; set; }
		public int CommentUnknownCount { get; set; }
		public int ExtensionInstallationCount { get; set; }
		public int ExtensionRemovalCount { get; set; }
	}
}