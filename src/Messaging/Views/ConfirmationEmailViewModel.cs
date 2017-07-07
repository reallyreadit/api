using System;
using api.Configuration;

namespace api.Messaging.Views {
	public class ConfirmationEmailViewModel : EmailLayoutViewModel {
		public ConfirmationEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			string name,
			string token,
			HttpEndpointOptions apiServerEndpoint
		) : base(title, webServerEndpoint) {
			Name = name;
			Token = token;
			ApiServerEndpoint = apiServerEndpoint;
		}
		public string Name { get; }
		public string Token { get; }
		public HttpEndpointOptions ApiServerEndpoint { get; }
	}
}