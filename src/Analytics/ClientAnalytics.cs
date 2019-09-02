namespace api.Analytics {
	public class ClientAnalytics {
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