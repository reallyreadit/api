namespace api.DataAccess.Models {
	public enum SubscriptionState {
		Incomplete,
		IncompleteExpired,
		Active,
		Lapsed,
		RenewalRequiresConfirmation,
		RenewalFailed
	}
}