using System;
using api.Analytics;
using Newtonsoft.Json;

namespace api.DataAccess.Serialization {
	public class ClientJsonConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return objectType == typeof(ClientAnalytics);
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			throw new NotSupportedException();
		}
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			var client = (ClientAnalytics)value;
			serializer.Serialize(
				jsonWriter: writer,
				value: new {
					Type = client.Type,
					Mode = client.Mode
				}
			);
		}
	}
}