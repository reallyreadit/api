using System.Threading.Tasks;
using api.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Mvc.RenderViewToString;
using api.BackgroundProcessing;

namespace api.Messaging {
	public class SmtpEmailService: EmailService {
		private readonly SmtpServerOptions smtpOptions;
		public SmtpEmailService(
			IOptions<DatabaseOptions> dbOpts,
			RazorViewToStringRenderer viewRenderer,
			IOptions<EmailOptions> emailOpts,
			IOptions<ServiceEndpointsOptions> serviceOpts,
			IOptions<TokenizationOptions> tokenizationOptions,
			IBackgroundTaskQueue taskQueue
		) : base(
			dbOpts,
			viewRenderer,
			emailOpts,
			serviceOpts,
			tokenizationOptions,
			taskQueue
		) {
			smtpOptions = emailOpts.Value.SmtpServer;
		}
		public override async Task Send(params EmailMessage[] messages) {
			using (var client = new SmtpClient()) {
				await client.ConnectAsync(smtpOptions.Host, smtpOptions.Port);
				foreach (var message in messages) {
					var mimeMessage = new MimeMessage(
						from: new[] { new MailboxAddress(message.From.Name, message.From.Address) },
						to: new [] { new MailboxAddress(message.To.Name, message.To.Address) },
						subject: message.Subject,
						body: new TextPart(TextFormat.Html) {
							Text = message.Body
						}
					);
					if (message.ReplyTo != null) {
						mimeMessage.ReplyTo.Add(new MailboxAddress(message.ReplyTo.Name, message.ReplyTo.Address));
					}
					await client.SendAsync(mimeMessage);
				}
				await client.DisconnectAsync(quit: true);
			}
		}
	}
}