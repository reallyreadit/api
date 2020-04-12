using System;

namespace api.DataAccess.Models {
	public class DailyTotalsReportRow {
		public DateTime Day { get; set; }
		public int SignupAppCount { get; set; }
		public int SignupBrowserCount { get; set; }
		public int SignupUnknownCount { get; set; }
		public int ReadAppCount { get; set; }
		public int ReadBrowserCount { get; set; }
		public int ReadUnknownCount { get; set; }
		public int PostAppCount { get; set; }
		public int PostBrowserCount { get; set; }
		public int PostUnknownCount { get; set; }
		public int ReplyAppCount { get; set; }
		public int ReplyBrowserCount { get; set; }
		public int ReplyUnknownCount { get; set; }
		public int PostTweetAppCount { get; set; }
		public int PostTweetBrowserCount { get; set; }
		public int ExtensionInstallationCount { get; set; }
		public int ExtensionRemovalCount { get; set; }
	}
}