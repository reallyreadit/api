namespace api.Analytics {
	public class RequestAnalytics {
		public RequestAnalytics(
			ClientAnalytics client
		) {
			Client = client;
		}
		public ClientAnalytics Client { get; }
	}
}