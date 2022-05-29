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
	public enum AppleSubscriptionValidationResponseType {
		AssociatedWithCurrentUser = 1,
		AssociatedWithAnotherUser = 2,
		EmptyReceipt = 3
	}
	public abstract class AppleSubscriptionValidationResponse {
		public AppleSubscriptionValidationResponse(
			AppleSubscriptionValidationResponseType type
		) {
			Type = type;
		}
		public AppleSubscriptionValidationResponseType Type { get; }
	}
	public class AppleSubscriptionAssociatedWithCurrentUserResponse :
		AppleSubscriptionValidationResponse
	{
		public AppleSubscriptionAssociatedWithCurrentUserResponse(
			SubscriptionStatusClientModel subscriptionStatus
		) :
			base(AppleSubscriptionValidationResponseType.AssociatedWithCurrentUser)
		{
			SubscriptionStatus = subscriptionStatus;
		}
		public object SubscriptionStatus { get; }
	}
	public class AppleSubscriptionAssociatedWithAnotherUserResponse :
		AppleSubscriptionValidationResponse
	{
		public AppleSubscriptionAssociatedWithAnotherUserResponse(
			string subscribedUsername
		) :
			base(AppleSubscriptionValidationResponseType.AssociatedWithAnotherUser)
		{
			SubscribedUsername = subscribedUsername;
		}
		public string SubscribedUsername { get; }
	}
	public class AppleSubscriptionEmptyReceiptResponse :
		AppleSubscriptionValidationResponse
	{
		public AppleSubscriptionEmptyReceiptResponse() :
			base(AppleSubscriptionValidationResponseType.EmptyReceipt)
		{
		}
	}
}