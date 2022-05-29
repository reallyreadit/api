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

namespace api.DataAccess.Models {
	public class FreeTrialCredit {
		public long Id { get; set; }
		public DateTime DateCreated { get; set; }
		public long UserAccountId { get; set; }
		public FreeTrialCreditTrigger CreditTrigger { get; set; }
		public FreeTrialCreditType CreditType { get; set; }
		public int AmountCredited { get; set; }
		public int AmountRemaining { get; set; }
	}
}