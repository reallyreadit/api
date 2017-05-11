using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using Mvc.RenderViewToString;

namespace api.Messaging {
	public class EmailService {
		private RazorViewToStringRenderer viewRenderer;
		private EmailOptions emailOpts;
		private async Task SendEmail(EmailAddress to, string viewName, EmailViewModel model) {
			var from = new EmailAddress(emailOpts.FromName, emailOpts.FromAddress);
			var body = await this.viewRenderer.RenderViewToStringAsync(viewName, model);
			switch (emailOpts.DeliveryMethod) {
				case EmailDeliveryMethod.AmazonSes:
					await AmazonSesEmailService.SendEmail(from, to, model.Subject, body, emailOpts.AmazonSesRegionEndpoint);
					return;
				case EmailDeliveryMethod.Smtp:
					await SmtpEmailService.SendEmail(from, to, model.Subject, body, emailOpts.SmtpServer.Host, emailOpts.SmtpServer.Port);
					return;
			}
		}
		public EmailService(RazorViewToStringRenderer viewRenderer, IOptions<EmailOptions> emailOpts) {
			this.viewRenderer = viewRenderer;
			this.emailOpts = emailOpts.Value;
		}
		public async Task SendConfirmationEmail(EmailAddress recipient) {
			await SendEmail(recipient, "ConfirmationEmail", new ConfirmationEmailViewModel() {
				Subject = "Welcome to reallyread.it!",
				Name = recipient.Name
			});
		}
	}
}