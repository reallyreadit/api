namespace api.Analytics {
	public class RequestAnalytics {
		public RequestAnalytics(
			Client client,
			string context
		) {
			Client = client;
			Context = context;
		}
		public Client Client { get; }
		public string Context { get; }
	}
}