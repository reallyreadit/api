namespace api.Configuration {
	public class TwitterAuthOptions {
		public string BrowserAuthCallback { get; set; }
		public string BrowserLinkCallback { get; set; }
		public string ConsumerKey { get; set; }
		public string ConsumerSecret { get; set; }
		public TwitterAccountOptions SearchAccount { get; set; }
		public string TwitterApiServerUrl { get; set; }
		public string WebViewCallback { get; set; }
	}
}