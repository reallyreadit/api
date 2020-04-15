using System;
using api.Analytics;
using Newtonsoft.Json;

namespace api.DataAccess.Serialization {
	public class ClientTypeJsonConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return objectType == typeof(ClientType);
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			return ClientTypeDictionary.StringToEnum[reader.Value as string];
		}
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			writer.WriteValue(ClientTypeDictionary.EnumToString[(ClientType)value]);
		}
	}
}