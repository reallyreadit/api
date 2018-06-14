using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace api.Messaging {
	public static class SmtpEmailService {
		public static async Task<bool> SendEmail(EmailMailbox from, EmailMailbox replyTo, EmailMailbox to, string subject, string body, string host, int port) {
			using (var client = new SmtpClient()) {
				await client.ConnectAsync(host, port);
				var message = new MimeMessage(
					from: new[] { new MailboxAddress(from.Name, from.Address) },
					to: new [] { new MailboxAddress(to.Name, to.Address) },
					subject: subject,
					body: new TextPart(TextFormat.Html) {
						Text = body
					}
				);
				if (replyTo != null) {
					message.ReplyTo.Add(new MailboxAddress(replyTo.Name, replyTo.Address));
				}
				await client.SendAsync(message);
				await client.DisconnectAsync(quit: true);
				return true;
			}
		}
	}
}