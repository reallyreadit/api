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

namespace api.Controllers.Shared {
	public class AuthorProfileClientModel {
		public AuthorProfileClientModel(
			string name,
			string slug,
			int totalEarnings,
			int totalPayouts,
			string userName,
			DonationRecipient donationRecipient
		) {
			Name = name;
			Slug = slug;
			TotalEarnings = totalEarnings;
			TotalPayouts = totalPayouts;
			UserName = userName;
			if (donationRecipient != null) {
				DonationRecipient = new DonationRecipientClientModel(donationRecipient);
			}
		}
		public string Name { get; }
		public string Slug { get; }
		public int TotalEarnings { get; }
		public int TotalPayouts { get; }
		public string UserName { get; }
		public DonationRecipientClientModel DonationRecipient { get; }
	}
}