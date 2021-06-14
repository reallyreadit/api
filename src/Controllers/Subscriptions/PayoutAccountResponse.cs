using api.Controllers.Shared;

namespace api.Controllers.Subscriptions {
	public class PayoutAccountResponse {
		public PayoutAccountResponse(
			PayoutAccountClientModel payoutAccount
		) {
			PayoutAccount = payoutAccount;
		}
		public PayoutAccountClientModel PayoutAccount { get; }
	}
}