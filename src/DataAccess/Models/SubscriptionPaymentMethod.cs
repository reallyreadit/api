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
using api.Subscriptions;

namespace api.DataAccess.Models {
	public class SubscriptionPaymentMethod {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderPaymentMethodId { get; set; }
		public string ProviderAccountId { get; set; }
		public DateTime DateCreated { get; set; }
		public SubscriptionPaymentMethodWallet Wallet { get; set; }
		public SubscriptionPaymentMethodBrand Brand { get; set; }
		public string LastFourDigits { get; set; }
		public string Country { get; set; }
		public int ExpirationMonth { get; set; }
		public int ExpirationYear { get; set; }
	}
}