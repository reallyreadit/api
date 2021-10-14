using System;

namespace api.DataAccess.Models {
	public class FreeTrialArticleView {
		public long ArticleId { get; set; }
		public string ArticleSlug { get; set; }
		public DateTime DateViewed { get; set; }
		public long FreeTrialCreditId { get; set; }
	}
}