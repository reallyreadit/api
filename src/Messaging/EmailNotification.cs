using System;
using api.Formatting;

namespace api.Messaging {
	public class EmailNotification<TContent> {
		public EmailNotification(
			long userId,
			EmailMailbox to,
			string subject,
			Uri openUrl,
			TContent content
		) {
			UserId = userId;
			To = to;
			Subject = subject.RemoveControlCharacters();
			OpenUrl = openUrl;
			Content = content;
		}
		public EmailNotification(
			long userId,
			EmailMailbox replyTo,
			EmailMailbox to,
			string subject,
			Uri openUrl,
			TContent content
		) : this(
			userId,
			to,
			subject,
			openUrl,
			content
		) {
			ReplyTo = replyTo;
		}
		public long UserId { get; }
		public EmailMailbox ReplyTo { get; }
		public EmailMailbox To { get; }
		public string Subject { get; }
		public Uri OpenUrl { get; }
		public TContent Content { get; }
		public string Subscription { get; }
	}
}