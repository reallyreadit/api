using System;
using api.Configuration;

namespace api.Messaging.Views {
	public class ConfirmationEmailViewModel : EmailLayoutViewModel {
		public ConfirmationEmailViewModel(
			string title,
			HttpEndpointOptions webServerEndpoint,
			string name,
			Guid emailConfirmationId,
			HttpEndpointOptions apiServerEndpoint
		) : base(title, webServerEndpoint) {
			Name = name;
			EmailConfirmationId = emailConfirmationId;
			ApiServerEndpoint = apiServerEndpoint;
		}
		public string Name { get; }
		public Guid EmailConfirmationId { get; }
		public HttpEndpointOptions ApiServerEndpoint { get; }
	}
}