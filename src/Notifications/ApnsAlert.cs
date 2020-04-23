using api.Formatting;

namespace api.Notifications {
	// total payload size is 4KB
	// 1 character = 1 byte when converted to json for transmission (i think)
	// budget ~500B for baseline payload
	// title = 500
	// subtitle = 500
	// body = 2000
	public class ApnsAlert {
		public ApnsAlert(
			string title
		) {
			Title = title
				.RemoveControlCharacters()
				.Truncate(500, appendEllipsis: false);
		}
		public ApnsAlert(
			string title,
			string body
		) : this(
			title
		) {
			Body = body.Truncate(2000, appendEllipsis: false);
		}
		public ApnsAlert(
			string title,
			string subtitle,
			string body
		) : this(
			title,
			body
		) {
			Subtitle = subtitle
				.RemoveControlCharacters()
				.Truncate(500, appendEllipsis: false);
		}
		public string Title { get; }
		public string Subtitle { get; }
		public string Body { get; }
	}
}