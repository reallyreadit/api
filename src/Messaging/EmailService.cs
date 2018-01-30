using System;
using System.Linq;
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
using api.DataAccess;
using System.Collections.Generic;
using Npgsql;

namespace api.Messaging {
	public class EmailService {
		private Lazy<IEnumerable<string>> bouncedAddresses;
		private RazorViewToStringRenderer viewRenderer;
		private EmailOptions emailOpts;
		private ServiceEndpointsOptions serviceOpts;
		private static string NormalizeEmailAddress(string address) => String.IsNullOrWhiteSpace(address) ?
			String.Empty :
			address.ToLower().Trim();
		private string CreateToken(object value) => WebUtility.UrlEncode(StringEncryption.Encrypt(value?.ToString(), emailOpts.EncryptionKey));
		private async Task<bool> SendEmail(UserAccount recipient, string viewName, EmailLayoutViewModel model, bool requireConfirmation = true) {
			if (!CanSendEmailTo(recipient, requireConfirmation)) {
				return false;
			}
			EmailMailbox
				from = new EmailMailbox(emailOpts.From.Name, emailOpts.From.Address),
				to = new EmailMailbox(recipient.Name, recipient.Email);
			var body = await this.viewRenderer.RenderViewToStringAsync(viewName, model);
			switch (emailOpts.DeliveryMethod) {
				case EmailDeliveryMethod.AmazonSes:
					return await AmazonSesEmailService.SendEmail(from, to, model.Title, body, emailOpts.AmazonSesRegionEndpoint);
				case EmailDeliveryMethod.Smtp:
					return await SmtpEmailService.SendEmail(from, to, model.Title, body, emailOpts.SmtpServer.Host, emailOpts.SmtpServer.Port);
				default:
					throw new InvalidOperationException("Unexpected value for DeliveryMethod option");
			}
		}
		public EmailService(IOptions<DatabaseOptions> dbOpts, RazorViewToStringRenderer viewRenderer, IOptions<EmailOptions> emailOpts, IOptions<ServiceEndpointsOptions> serviceOpts) {
			this.bouncedAddresses = new Lazy<IEnumerable<string>>(
				() => {
					using (var db = DbApi.CreateConnection(dbOpts.Value.ConnectionString)) {
						return db
							.ListEmailBounces()
							.Select(bounce => NormalizeEmailAddress(bounce.Address))
							.ToArray();
					}
				}
			);
			this.viewRenderer = viewRenderer;
			this.emailOpts = emailOpts.Value;
			this.serviceOpts = serviceOpts.Value;
		}
		public bool CanSendEmailTo(UserAccount account, bool requireConfirmation = true) =>
			!this.bouncedAddresses.Value.Contains(NormalizeEmailAddress(account.Email)) &&
			(requireConfirmation ? account.IsEmailConfirmed : true);
		public async Task<bool> SendWelcomeEmail(UserAccount recipient, Guid emailConfirmationId) => await SendEmail(
			recipient,
			viewName: "WelcomeEmail",
			model: new ConfirmationEmailViewModel(
				title: "Welcome to reallyread.it!",
				webServerEndpoint: this.serviceOpts.WebServer,
				name: recipient.Name,
				token: CreateToken(emailConfirmationId),
				apiServerEndpoint: this.serviceOpts.ApiServer
			),
			requireConfirmation: false
		);
		public async Task<bool> SendConfirmationEmail(UserAccount recipient, Guid emailConfirmationId) => await SendEmail(
			recipient,
			viewName: "ConfirmationEmail",
			model: new ConfirmationEmailViewModel(
				title: "Please confirm your email address",
				webServerEndpoint: this.serviceOpts.WebServer,
				name: recipient.Name,
				token: CreateToken(emailConfirmationId),
				apiServerEndpoint: this.serviceOpts.ApiServer
			),
			requireConfirmation: false
		);
		public async Task<bool> SendPasswordResetEmail(UserAccount recipient, Guid passwordResetRequestId) => await SendEmail(
			recipient,
			viewName: "PasswordResetEmail",
			model: new ConfirmationEmailViewModel(
				title: "Password reset request",
				webServerEndpoint: this.serviceOpts.WebServer,
				name: recipient.Name,
				token: CreateToken(passwordResetRequestId),
				apiServerEndpoint: this.serviceOpts.ApiServer
			),
			requireConfirmation: false
		);
		public async Task<bool> SendCommentReplyNotificationEmail(UserAccount recipient, Comment reply) => await SendEmail(
			recipient,
			viewName: "ReplyNotificationEmail",
			model: new ReplyNotificationEmailViewModel(
				title: $"{reply.UserAccount} replied to your comment!",
				webServerEndpoint: this.serviceOpts.WebServer,
				subscriptionsToken: CreateToken(recipient.Id),
				respondent: reply.UserAccount,
				articleTitle: reply.ArticleTitle,
				replyToken: CreateToken(reply.Id),
				apiServerEndpoint: this.serviceOpts.ApiServer
			),
			requireConfirmation: true
		);
		public async Task<bool> SendBulkMailingEmail(UserAccount recipient, string list, string subject, string body, string listDescription) => await SendEmail(
			recipient,
			viewName: "BulkMailingEmail",
			model: new BulkMailingEmailViewModel(
				title: subject,
				webServerEndpoint: this.serviceOpts.WebServer,
				body: body,
				listDescription: listDescription,
				subscriptionsToken: CreateToken(recipient.Id)
			),
			requireConfirmation: true
		);
	}
}