// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using api.Controllers.Shared;

namespace api.Controllers.Subscriptions {
	public enum PayoutAccountOnboardingLinkRequestResponseType {
		ReadyForOnboarding = 1,
		OnboardingCompleted = 2
	}
	public abstract class PayoutAccountOnboardingLinkRequestResponse {
		public PayoutAccountOnboardingLinkRequestResponse(
			PayoutAccountOnboardingLinkRequestResponseType type
		) {
			Type = type;
		}
		public PayoutAccountOnboardingLinkRequestResponseType Type { get; }
	}
	public class PayoutAccountOnboardingLinkRequestReadyResponse :
		PayoutAccountOnboardingLinkRequestResponse
	{
		public PayoutAccountOnboardingLinkRequestReadyResponse(
			string onboardingUrl
		) : base(
			type: PayoutAccountOnboardingLinkRequestResponseType.ReadyForOnboarding
		) {
			OnboardingUrl = onboardingUrl;
		}
		public string OnboardingUrl { get; }
	}
	public class PayoutAccountOnboardingLinkRequestCompletedResponse :
		PayoutAccountOnboardingLinkRequestResponse
	{
		public PayoutAccountOnboardingLinkRequestCompletedResponse(
			PayoutAccountClientModel payoutAccount
		) : base(
			type: PayoutAccountOnboardingLinkRequestResponseType.OnboardingCompleted
		) {
			PayoutAccount = payoutAccount;
		}
		public PayoutAccountClientModel PayoutAccount { get; }
	}
}