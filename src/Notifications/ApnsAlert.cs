namespace api.Notifications {
	public class ApnsAlert {
		public ApnsAlert(
			string title
		) {
			Title = title;
		}
		public ApnsAlert(
			string title,
			string body
		) {
			Title = title;
			Body = body;
		}
		public ApnsAlert(
			string title,
			string subtitle,
			string body
		) {
			Title = title;
			Subtitle = subtitle;
			Body = body;
		}
		public string Title { get; }
		public string Subtitle { get; }
		public string Body { get; }
	}
}