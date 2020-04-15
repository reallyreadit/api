using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace api.DataAccess.Serialization {
	public static class PostgresJsonSerialization {
		private static JsonSerializerSettings settings = new JsonSerializerSettings() {
			ContractResolver = new DefaultContractResolver() {
				NamingStrategy = new SnakeCaseNamingStrategy()
			},
			Converters = {
				new ClientTypeJsonConverter(),
				new SemanticVersionJsonConverter()
			}
		};
		public static T Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value, settings);
		public static string Serialize(object value) => JsonConvert.SerializeObject(value, settings);
	}
}