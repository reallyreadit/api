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