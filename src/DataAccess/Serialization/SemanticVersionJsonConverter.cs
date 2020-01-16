using System;
using api.Analytics;
using Newtonsoft.Json;

namespace api.DataAccess.Serialization {
	public class SemanticVersionJsonConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return objectType == typeof(SemanticVersion);
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			throw new NotSupportedException();
		}
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			writer.WriteValue(((SemanticVersion)value)?.ToString());
		}
	}
}