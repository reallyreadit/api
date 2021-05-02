using System;

namespace api.Messaging.Views {
	public class InitialSubscriptionEmailViewModel {
		public InitialSubscriptionEmailViewModel(
			string userName,
			Uri billProfileUrl,
			Uri jeffProfileUrl
		) {
			UserName = userName;
			BillProfileUrl = billProfileUrl.ToString();
			JeffProfileUrl = jeffProfileUrl.ToString();
		}
		public string UserName { get; }
		public string BillProfileUrl { get; }
		public string JeffProfileUrl { get; }
	}
}