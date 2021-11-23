namespace api.Messaging.Views {
	public class InitialSubscriptionEmailViewModel {
		public InitialSubscriptionEmailViewModel(
			string userName
		) {
			UserName = userName;
		}
		public string UserName { get; }
	}
}