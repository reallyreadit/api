using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace api.Messaging {
	public static class SmtpEmailService {
		public static async Task SendEmail(EmailMailbox from, EmailMailbox to, string subject, string body, string host, int port) {
			using (var client = new SmtpClient()) {
				await client.ConnectAsync(host, port);
				await client.SendAsync(new MimeMessage(
					from: new[] { new MailboxAddress(from.Name, from.Address) },
					to: new [] { new MailboxAddress(to.Name, to.Address) },
					subject: subject,
					body: new TextPart(TextFormat.Html) {
						Text = body
					}
				));
				await client.DisconnectAsync(quit: true);
			}
		}
	}
}