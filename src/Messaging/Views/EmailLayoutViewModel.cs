using api.Configuration;

namespace api.Messaging.Views {
	public class EmailLayoutViewModel {
		public EmailLayoutViewModel(string title, HttpEndpointOptions webServerEndpoint) {
			Title = title;
			WebServerEndpoint = webServerEndpoint;
		}
		public string Title { get; }
		public HttpEndpointOptions WebServerEndpoint { get;}
	}
}