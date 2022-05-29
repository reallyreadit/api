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
using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public enum SubscriptionProviderClientValue {
		Apple = 1,
		Stripe = 2
	}
	public static class SubscriptionProviderClientValueExtensions {
		public static SubscriptionProviderClientValue FromSubscriptionProvider(
			SubscriptionProvider provider
		) {
			switch (provider) {
				case SubscriptionProvider.Apple:
					return SubscriptionProviderClientValue.Apple;
				case SubscriptionProvider.Stripe:
					return SubscriptionProviderClientValue.Stripe;
			}
			throw new ArgumentOutOfRangeException($"Unexpected value for provider: {provider}");
		}
		public static SubscriptionProvider ToSubscriptionProvider(
			this SubscriptionProviderClientValue provider
		) {
			switch (provider) {
				case SubscriptionProviderClientValue.Apple:
					return SubscriptionProvider.Apple;
				case SubscriptionProviderClientValue.Stripe:
					return SubscriptionProvider.Stripe;
			}
			throw new ArgumentOutOfRangeException($"Unexpected value for provider: {provider}");
		}
	}
}