using System;
using api.Configuration;

namespace api.Messaging.Views {
	public class ConfirmationReminderEmailViewModel : EmailLayoutViewModel {
		public ConfirmationReminderEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			string body,
			string confirmationToken,
			string subscriptionsToken,
			HttpEndpointOptions apiServerEndpoint
		) : base(title, webServerEndpoint) {
			Body = body;
			ConfirmationToken = confirmationToken;
			SubscriptionsToken = subscriptionsToken;
			ApiServerEndpoint = apiServerEndpoint;
		}
		public string Body { get; }
		public string ConfirmationToken { get; }
		public string SubscriptionsToken { get; }
		public HttpEndpointOptions ApiServerEndpoint { get; }
	}
}