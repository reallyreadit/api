using System;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using Mvc.RenderViewToString;
using api.Messaging.Views;

namespace api.Messaging {
	public class EmailService {
		private RazorViewToStringRenderer viewRenderer;
		private EmailOptions emailOpts;
		private ServiceEndpointsOptions serviceOpts;
		private async Task SendEmail(EmailMailbox to, string viewName, EmailLayoutViewModel model) {
			var from = new EmailMailbox(emailOpts.From.Name, emailOpts.From.Address);
			var body = await this.viewRenderer.RenderViewToStringAsync(viewName, model);
			switch (emailOpts.DeliveryMethod) {
				case EmailDeliveryMethod.AmazonSes:
					await AmazonSesEmailService.SendEmail(from, to, model.Title, body, emailOpts.AmazonSesRegionEndpoint);
					return;
				case EmailDeliveryMethod.Smtp:
					await SmtpEmailService.SendEmail(from, to, model.Title, body, emailOpts.SmtpServer.Host, emailOpts.SmtpServer.Port);
					return;
			}
		}
		public EmailService(RazorViewToStringRenderer viewRenderer, IOptions<EmailOptions> emailOpts, IOptions<ServiceEndpointsOptions> serviceOpts) {
			this.viewRenderer = viewRenderer;
			this.emailOpts = emailOpts.Value;
			this.serviceOpts = serviceOpts.Value;
		}
		public async Task SendConfirmationEmail(EmailMailbox recipient, Guid emailConfirmationId) => await SendEmail(
			to: recipient,
			viewName: "ConfirmationEmail",
			model: new ConfirmationEmailViewModel(
				title: "Welcome to reallyread.it!",
				webServerEndpoint: this.serviceOpts.WebServer,
				name: recipient.Name,
				emailConfirmationId: emailConfirmationId,
				apiServerEndpoint: this.serviceOpts.ApiServer
			)
		);
	}
}