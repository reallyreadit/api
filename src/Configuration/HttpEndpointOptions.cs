using System.Collections.Generic;
using api.Serialization;

namespace api.Configuration {
	public class HttpEndpointOptions {
		public string Protocol { get; set; }
		public string Host { get; set; }
		public int? Port { get; set; }
		public string CreateUrl(
			string path = null,
			IEnumerable<KeyValuePair<string, string>> query = null
		) => (
			$"{Protocol}://{Host}{(Port.HasValue ? ":" + Port : null)}{path}{QueryStringSerializer.Serialize(query, includePrefix: true)}"
		);
	}
}