namespace api.Configuration {
	public class TwitterAuthOptions {
		public TwitterBotOptions Bots { get; set; }
		public string BrowserCallback { get; set; }
		public string ConsumerKey { get; set; }
		public string ConsumerSecret { get; set; }
		public string WebViewCallback { get; set; }
	}
}