// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System.Text.RegularExpressions;

namespace api.Analytics {
	public class ClientAnalytics {
		public static ClientAnalytics ParseClientString(string value) {
			var match = Regex.Match(
				input: value,
				pattern: @"([a-z\-/]+)(#\w+)?@(\d+\.\d+\.\d+)$"
			);
			if (match.Success && ClientTypeDictionary.StringToEnum.ContainsKey(match.Groups[1].Value)) {
				return new ClientAnalytics(
					type: ClientTypeDictionary.StringToEnum[match.Groups[1].Value],
					version: new SemanticVersion(match.Groups[3].Value),
					mode: match.Groups[2].Success ?
						match.Groups[2].Value.TrimStart('#') :
						null
				);
			}
			return null;
		}
		public ClientAnalytics() { }
		public ClientAnalytics(ClientType type, SemanticVersion version, string mode = null) {
			Type = type;
			Version = version;
			Mode = mode;
		}
		public ClientType Type { get; set; }
		public SemanticVersion Version { get; set; }
		public string Mode { get; set; }
	}
}