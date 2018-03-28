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
		private string CreateToken(object value) => WebUtility.UrlEncode(StringEncryption.Encrypt(value?.ToString(), emailOpts.EncryptionKey));
		private async Task<bool> SendEmail(IEmailRecipient recipient, string viewName, EmailLayoutViewModel model, bool requireConfirmation = true) {
			if (
				(requireConfirmation && !recipient.IsEmailAddressConfirmed) ||
				(HasEmailAddressBounced(recipient.EmailAddress))
			) {
				return false;
			}
			EmailMailbox
				from = new EmailMailbox(emailOpts.From.Name, emailOpts.From.Address),
				to = new EmailMailbox(recipient.Name, recipient.EmailAddress);
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
		public static string NormalizeEmailAddress(string address) => (
			String.IsNullOrWhiteSpace(address) ?
				String.Empty :
				address.ToLower().Trim()
		);
		public EmailService(IOptions<DatabaseOptions> dbOpts, RazorViewToStringRenderer viewRenderer, IOptions<EmailOptions> emailOpts, IOptions<ServiceEndpointsOptions> serviceOpts) {
			this.bouncedAddresses = new Lazy<IEnumerable<string>>(
				() => {
					using (var db = new NpgsqlConnection(dbOpts.Value.ConnectionString)) {
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
		public bool HasEmailAddressBounced(string emailAddress) => (
			this.bouncedAddresses.Value.Contains(NormalizeEmailAddress(emailAddress))
		);
		public async Task<bool> SendWelcomeEmail(UserAccount recipient, Guid emailConfirmationId) => await SendEmail(
			recipient,
			viewName: "WelcomeEmail",
			model: new ConfirmationEmailViewModel(
				title: "Please confirm your email address",
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
		public async Task<bool> SendListSubscriptionEmail(UserAccount recipient, string subject, string body, string listDescription) => await SendEmail(
			recipient,
			viewName: "ListSubscriptionEmail",
			model: new ListSubscriptionEmailViewModel(
				title: subject,
				webServerEndpoint: this.serviceOpts.WebServer,
				body: body,
				listDescription: listDescription,
				subscriptionsToken: CreateToken(recipient.Id)
			),
			requireConfirmation: true
		);
		public async Task<bool> SendConfirmationReminderEmail(UserAccount recipient, string subject, string body, Guid emailConfirmationId) => await SendEmail(
			recipient,
			viewName: "ConfirmationReminderEmail",
			model: new ConfirmationReminderEmailViewModel(
				title: subject,
				webServerEndpoint: this.serviceOpts.WebServer,
				body: body,
				confirmationToken: CreateToken(emailConfirmationId),
				subscriptionsToken: CreateToken(recipient.Id),
				apiServerEndpoint: this.serviceOpts.ApiServer
			),
			requireConfirmation: false
		);
		public async Task<bool> SendShareEmail(UserAccount sender, IEmailRecipient recipient, UserArticle article, string message) => await SendEmail(
			recipient,
			viewName: "ShareEmail",
			model: new ShareEmailViewModel(
				title: $"{sender.Name} shared an article with you",
				webServerEndpoint: this.serviceOpts.WebServer,
				sender: sender,
				article: article,
				message: message
			),
			requireConfirmation: false
		);
	}
}