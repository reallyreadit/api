namespace api.Configuration {
	public class HttpEndpointOptions {
		public string Protocol { get; set; }
		public string Host { get; set; }
		public int? Port { get; set; }
		public string CreateUrl(string path = null) => $"{Protocol}://{Host}{(Port.HasValue ? ":" + Port : null)}{path}";
	}
}