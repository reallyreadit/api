namespace api.Serialization {
	public class PemSection {
		public PemSection(string type, string encodedBody) {
			Type = type;
			EncodedBody = encodedBody;
		}
		public string Type { get; }
		public string EncodedBody { get; }
	}
}