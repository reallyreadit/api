using System;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using Mvc.RenderViewToString;
using api.Messaging.Views;
using api.Encryption;
using System.Net;
using api.DataAccess.Models;

namespace api.Messaging {
	public class EmailService {
		private RazorViewToStringRenderer viewRenderer;
		private EmailOptions emailOpts;
		private ServiceEndpointsOptions serviceOpts;
		private string CreateToken(object value) => WebUtility.UrlEncode(StringEncryption.Encrypt(value?.ToString(), emailOpts.EncryptionKey));
		private async Task SendEmail(UserAccount recipient, string viewName, EmailLayoutViewModel model) {
			EmailMailbox
				from = new EmailMailbox(emailOpts.From.Name, emailOpts.From.Address),
				to = new EmailMailbox(recipient.Name, recipient.Email);
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
		public async Task SendWelcomeEmail(UserAccount recipient, Guid emailConfirmationId) => await SendEmail(
			recipient,
			viewName: "WelcomeEmail",
			model: new ConfirmationEmailViewModel(
				title: "Welcome to reallyread.it!",
				webServerEndpoint: this.serviceOpts.WebServer,
				name: recipient.Name,
				emailConfirmationToken: CreateToken(emailConfirmationId),
				apiServerEndpoint: this.serviceOpts.ApiServer
			)
		);
		public async Task SendConfirmationEmail(UserAccount recipient, Guid emailConfirmationId) => await SendEmail(
			recipient,
			viewName: "ConfirmationEmail",
			model: new ConfirmationEmailViewModel(
				title: "Please confirm your email address",
				webServerEndpoint: this.serviceOpts.WebServer,
				name: recipient.Name,
				emailConfirmationToken: CreateToken(emailConfirmationId),
				apiServerEndpoint: this.serviceOpts.ApiServer
			)
		);
		public async Task SendCommentReplyNotificationEmail(UserAccount recipient, Comment reply) => await SendEmail(
			recipient,
			viewName: "ReplyNotificationEmail",
			model: new ReplyNotificationEmailViewModel(
				title: $"{reply.UserAccount} replied to your comment!",
				webServerEndpoint: this.serviceOpts.WebServer,
				unsubscribeToken: CreateToken(recipient.Id),
				respondent: reply.UserAccount,
				articleTitle: reply.ArticleTitle,
				replyToken: CreateToken(reply.Id),
				apiServerEndpoint: this.serviceOpts.ApiServer
			)
		);
	}
}