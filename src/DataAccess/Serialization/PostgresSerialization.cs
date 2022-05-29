// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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