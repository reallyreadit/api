using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public class FreeTrialClientModel {
		public FreeTrialClientModel(
			IEnumerable<FreeTrialCredit> credits,
			IEnumerable<FreeTrialArticleView> views
		) {
			Credits = credits
				.Select(
					credit => new FreeTrialCreditClientModel(credit)
				)
				.ToArray();
			ArticleViews = views
				.Select(
					view => new FreeTrialArticleViewClientModel(view)
				)
				.ToArray();
		}
		public FreeTrialCreditClientModel[] Credits { get; }
		public FreeTrialArticleViewClientModel[] ArticleViews { get; }
	}
}