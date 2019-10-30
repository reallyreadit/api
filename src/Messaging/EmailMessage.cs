namespace api.Messaging {
	public class EmailMessage {
		public EmailMessage(
			EmailMailbox from,
			EmailMailbox replyTo,
			EmailMailbox to,
			string subject,
			string body
		) {
			From = from;
			ReplyTo = replyTo;
			To = to;
			Subject = subject;
			Body = body;
		}
		public EmailMailbox From { get; }
		public EmailMailbox ReplyTo { get; }
		public EmailMailbox To { get; }
		public string Subject { get; }
		public string Body { get; }
	}
}