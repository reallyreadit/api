// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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