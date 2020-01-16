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
		public ClientAnalytics(ClientType type, SemanticVersion version, string mode = null) {
			Type = type;
			Version = version;
			Mode = mode;
		}
		public ClientType Type { get; }
		public SemanticVersion Version { get; }
		public string Mode { get; }
	}
}