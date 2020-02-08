using api.Formatting;

namespace api.Notifications {
	public class ApnsAlert {
		public ApnsAlert(
			string title
		) {
			Title = title.RemoveControlCharacters();
		}
		public ApnsAlert(
			string title,
			string body
		) : this(
			title
		) {
			Body = body;
		}
		public ApnsAlert(
			string title,
			string subtitle,
			string body
		) : this(
			title,
			body
		) {
			Subtitle = subtitle.RemoveControlCharacters();
		}
		public string Title { get; }
		public string Subtitle { get; }
		public string Body { get; }
	}
}