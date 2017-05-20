using System;
using api.Configuration;

namespace api.Messaging.Views {
	public class ConfirmationEmailViewModel : EmailLayoutViewModel {
		public ConfirmationEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			string name,
			string emailConfirmationToken,
			HttpEndpointOptions apiServerEndpoint
		) : base(title, webServerEndpoint) {
			Name = name;
			EmailConfirmationToken = emailConfirmationToken;
			ApiServerEndpoint = apiServerEndpoint;
		}
		public string Name { get; }
		public string EmailConfirmationToken { get; }
		public HttpEndpointOptions ApiServerEndpoint { get; }
	}
}