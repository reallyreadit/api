using System;
using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public class FreeTrialArticleViewClientModel {
		public FreeTrialArticleViewClientModel(FreeTrialArticleView view) {
			ArticleId = view.ArticleId;
			ArticleSlug = view.ArticleSlug;
			DateViewed = view.DateViewed;
		}
		public long ArticleId { get; }
		public string ArticleSlug { get; }
		public DateTime DateViewed { get; }
	}
}