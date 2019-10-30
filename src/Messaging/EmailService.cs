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
using System.Text.RegularExpressions;

namespace api.Messaging {
	public abstract class EmailService {
		private Lazy<IEnumerable<string>> bouncedAddresses;
		private RazorViewToStringRenderer viewRenderer;
		private EmailOptions emailOpts;
		private ServiceEndpointsOptions serviceOpts;
		private readonly TokenizationOptions tokenizationOptions;
		public static string NormalizeEmailAddress(string address) => (
			String.IsNullOrWhiteSpace(address) ?
				String.Empty :
				address.ToLower().Trim()
		);
		public EmailService(
			IOptions<DatabaseOptions> dbOpts,
			RazorViewToStringRenderer viewRenderer,
			IOptions<EmailOptions> emailOpts,
			IOptions<ServiceEndpointsOptions> serviceOpts,
			IOptions<TokenizationOptions> tokenizationOptions
		) {
			this.bouncedAddresses = new Lazy<IEnumerable<string>>(
				() => {
					using (var db = new NpgsqlConnection(dbOpts.Value.ConnectionString)) {
						return db
							.GetBlockedEmailAddresses()
							.Select(EmailFormatting.ExtractEmailAddress)
							.Where(address => address != null)
							.ToArray();
					}
				}
			);
			this.viewRenderer = viewRenderer;
			this.emailOpts = emailOpts.Value;
			this.serviceOpts = serviceOpts.Value;
			this.tokenizationOptions = tokenizationOptions.Value;
		}
		private string CreateSubscriptionsUrl(long userId) {
			var token = WebUtility.UrlEncode(StringEncryption.Encrypt(userId.ToString(), tokenizationOptions.EncryptionKey));
			return serviceOpts.WebServer.CreateUrl($"/email/subscriptions?token={token}");
		}
		private async Task Send<TContent>(string viewName, string subscription, params EmailNotification<TContent>[] notifications) {
			var sendableNotifications = notifications
				.Where(view => !HasEmailAddressBounced(view.To.Address))
				.ToArray();
			if (sendableNotifications.Any()) {
				var messages = new List<EmailMessage>();
				foreach (var notification in sendableNotifications) {
					messages.Add(
						new EmailMessage(
							from: new EmailMailbox(emailOpts.From.Name, emailOpts.From.Address),
							replyTo: notification.ReplyTo,
							to: notification.To,
							subject: notification.Subject,
							body: await this.viewRenderer.RenderViewToStringAsync(
								name: viewName,
								model: new LayoutViewModel<TContent>(
									title: notification.Subject,
									homeUrl: serviceOpts.WebServer.CreateUrl(),
									logoUrl: serviceOpts.StaticContentServer.CreateUrl("/email/logo.svg"),
									openImageUrl: notification.OpenUrl.ToString(),
									subscription: subscription,
									subscriptionsUrl: (
										subscription != null ?
											CreateSubscriptionsUrl(notification.UserId) :
											null
									),
									content: notification.Content
								)
							)
						)
					);
				}
				await Send(messages.ToArray());
			}
		}
		protected abstract Task Send(params EmailMessage[] messages);
		public bool HasEmailAddressBounced(string emailAddress) => (
			this.bouncedAddresses.Value.Contains(NormalizeEmailAddress(emailAddress))
		);
		public async Task SendReplyDigestNotifications(EmailNotification<CommentViewModel[]>[] notifications) {
			await Send("ReplyDigest", "reply digest notifications", notifications);
		}
		public async Task SendReplyNotification(EmailNotification<CommentViewModel> notification) {
			await Send("Reply", "reply notifications", notification);
		}
	}
}