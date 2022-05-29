// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using api.DataAccess.Models;

namespace api.Controllers.Subscriptions {
	public class AuthorEarningsReportLineItemClientModel {
		public AuthorEarningsReportLineItemClientModel(
			AuthorEarningsRanking ranking,
			Article topArticle
		) {
			AuthorName = ranking.AuthorName;
			AuthorSlug = ranking.AuthorSlug;
			UserAccountName = ranking.UserAccountName;
			DonationRecipientName = ranking.DonationRecipientName;
			MinutesRead = ranking.MinutesRead;
			AmountEarned = ranking.AmountEarned;
			if (ranking.DonationRecipientId.HasValue) {
				Status = AuthorEarningsReportLineItemStatus.DonationPaidOut;
			} else if (ranking.AmountPaid > 0) {
				Status = AuthorEarningsReportLineItemStatus.AuthorPaidOut;
			} else if (ranking.AuthorContactStatus == AuthorContactStatus.Attempted) {
				Status = AuthorEarningsReportLineItemStatus.Contacted;
			} else if (ranking.AmountEarned >= 1000) {
				Status = AuthorEarningsReportLineItemStatus.NotYetContacted;
			} else {
				Status = AuthorEarningsReportLineItemStatus.ApproachingMinimum;
			}
			TopArticle = topArticle;
		}
		public string AuthorName { get; }
		public string AuthorSlug { get; }
		public string UserAccountName { get; }
		public string DonationRecipientName { get; }
		public int MinutesRead { get; }
		public int AmountEarned { get; }
		public AuthorEarningsReportLineItemStatus Status { get; }
		public Article TopArticle { get; }
	}
}