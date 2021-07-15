using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace api.DataAccess.Serialization {
	public static class PostgresSerialization {
		private static JsonSerializerSettings settings = new JsonSerializerSettings() {
			ContractResolver = new DefaultContractResolver() {
				NamingStrategy = new SnakeCaseNamingStrategy()
			},
			Converters = {
				new ClientTypeJsonConverter(),
				new SemanticVersionJsonConverter()
			}
		};
		public static T DeserializeJson<T>(string value) => JsonConvert.DeserializeObject<T>(value, settings);
		public static string SerializeJson(object value) => JsonConvert.SerializeObject(value, settings);
		public static string SerializeEnum(Enum value) => (
			value != null ?
				Regex.Replace(
					input: value.ToString(),
					pattern: "([a-z])?([A-Z])",
					evaluator: match => (
							match.Groups[1].Success ?
								match.Groups[1].Value + "_" :
								String.Empty
						) +
						match.Groups[2].Value.ToLower()
				) :
				null
		);
	}
}