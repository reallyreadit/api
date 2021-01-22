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