using System;

namespace api.DataAccess.Models {
	public class ArticleIssuesReportRow {
		public DateTime DateCreated { get; set; }
		public string ArticleUrl { get; set; }
		public int ArticleAotdContenderRank { get; set; }
		public string UserName { get; set; }
		public string Issue { get; set; }
		public string ClientType { get; set; }
	}
}