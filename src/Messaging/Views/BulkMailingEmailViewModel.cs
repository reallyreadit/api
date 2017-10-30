using System;
using api.Configuration;

namespace api.Messaging.Views {
	public class BulkMailingEmailViewModel : EmailLayoutViewModel {
		public BulkMailingEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			string body,
			string listDescription,
			string subscriptionsToken
		) : base(title, webServerEndpoint) {
			Body = body;
			ListDescription = listDescription;
			SubscriptionsToken = subscriptionsToken;
		}
		public string Body { get; }
		public string ListDescription { get; }
		public string SubscriptionsToken { get; }
	}
}