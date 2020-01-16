using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace api.DataAccess.Serialization {
	public static class PostgresJsonSerialization {
		public static string Serialize(object value) => JsonConvert.SerializeObject(
			value: value,
			settings: new JsonSerializerSettings() {
				ContractResolver = new DefaultContractResolver() {
					NamingStrategy = new SnakeCaseNamingStrategy()
				},
				Converters = {
					new ClientTypeJsonConverter(),
					new SemanticVersionJsonConverter()
				}
			}
		);
	}
}