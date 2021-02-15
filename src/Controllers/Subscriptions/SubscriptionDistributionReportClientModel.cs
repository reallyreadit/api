using System;
using System.Linq;
using api.Controllers.Shared;
using api.DataAccess.Models;

namespace api.Controllers.Subscriptions {
	public class SubscriptionDistributionReportClientModel {
		public static SubscriptionDistributionReportClientModel Empty {
			get {
				return new SubscriptionDistributionReportClientModel();
			}
		}
		private SubscriptionDistributionReportClientModel() {
			AuthorDistributions = new SubscriptionDistributionAuthorReportClientModel[0];
		}
		public SubscriptionDistributionReportClientModel(
			SubscriptionDistributionReport report
		) {
			SubscriptionAmount = report.SubscriptionAmount;
			PlatformAmount = report.PlatformAmount;
			AppleAmount = report.AppleAmount;
			StripeAmount = report.StripeAmount;
			UnknownAuthorMinutesRead = report.UnknownAuthorMinutesRead;
			UnknownAuthorAmount = report.UnknownAuthorAmount;
			AuthorDistributions = report.AuthorDistributions
				.Select(
					authorDistribution => new SubscriptionDistributionAuthorReportClientModel(authorDistribution)
				)
				.ToArray();
		}
		public int SubscriptionAmount { get; }
		public int PlatformAmount { get; }
		public int AppleAmount { get; }
		public int StripeAmount { get; }
		public int UnknownAuthorMinutesRead { get; }
		public int UnknownAuthorAmount { get; }
		public SubscriptionDistributionAuthorReportClientModel[] AuthorDistributions { get; }
	}
}