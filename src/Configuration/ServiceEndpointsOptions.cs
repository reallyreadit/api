namespace api.Configuration {
	public class ServiceEndpointsOptions {
		public HttpEndpointOptions ApiServer { get; set; }
		public HttpEndpointOptions StaticContentServer { get; set; }
		public HttpEndpointOptions WebServer { get; set; }
	}
}