using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace api.Serialization {
	public static class JsonSnakeCaseSerializer {
		public static T Deserialize<T>(string json) => (
			JsonConvert.DeserializeObject<T>(
				value: json,
				settings: new JsonSerializerSettings() {
					ContractResolver = new DefaultContractResolver() {
						NamingStrategy = new SnakeCaseNamingStrategy()
					}
				}
			)
		);
	}
}