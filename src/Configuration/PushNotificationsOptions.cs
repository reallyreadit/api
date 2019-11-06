using Amazon;
using api.Messaging;

namespace api.Configuration {
	public class PushNotificationsOptions {
		public HttpEndpointOptions ApnsServer { get; set; }
		public string ApnsTopic { get; set; }
		public string ClientCertThumbprint { get; set; }
	}
}