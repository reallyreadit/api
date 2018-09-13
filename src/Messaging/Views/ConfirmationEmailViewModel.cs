using System;
using api.Configuration;

namespace api.Messaging.Views {
	public class ConfirmationEmailViewModel : EmailLayoutViewModel {
		public ConfirmationEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			string name,
			string token
		) : base(title, webServerEndpoint) {
			Name = name;
			Token = token;
		}
		public string Name { get; }
		public string Token { get; }
	}
}