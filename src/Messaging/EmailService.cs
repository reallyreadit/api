using System;
using System.Linq;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.Extensions.Options;
using Mvc.RenderViewToString;
using api.Messaging.Views;
using api.Encryption;
using System.Net;
using api.DataAccess;
using System.Collections.Generic;
using Npgsql;
using api.Messaging.Views.Shared;
using api.BackgroundProcessing;

namespace api.Messaging {
	public abstract class EmailService {
		private Lazy<IEnumerable<string>> bouncedAddresses;
		private RazorViewToStringRenderer viewRenderer;
		private EmailOptions emailOpts;
		private ServiceEndpointsOptions serviceOpts;
		private readonly TokenizationOptions tokenizationOptions;
		private readonly IBackgroundTaskQueue taskQueue;
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
			IOptions<TokenizationOptions> tokenizationOptions,
			IBackgroundTaskQueue taskQueue
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
			this.taskQueue = taskQueue;
		}
		private string CreateSubscriptionsUrl(long userId) {
			var token = WebUtility.UrlEncode(StringEncryption.Encrypt(userId.ToString(), tokenizationOptions.EncryptionKey));
			return serviceOpts.WebServer.CreateUrl($"/email/subscriptions?token={token}");
		}
		private void Send<TContent>(string viewName, string subscription, params EmailNotification<TContent>[] notifications) {
			taskQueue.QueueBackgroundWorkItem(
				async token => {
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
			);
		}
		protected abstract Task Send(params EmailMessage[] messages);
		public bool HasEmailAddressBounced(string emailAddress) => (
			this.bouncedAddresses.Value.Contains(NormalizeEmailAddress(emailAddress))
		);
		public void SendAotdDigestNotifications(EmailNotification<ArticleViewModel[]>[] notifications) {
			Send("AotdDigestEmail", "the Article of the Day digest", notifications);
		}
		public void SendAotdNotifications(EmailNotification<AotdEmailViewModel>[] notifications) {
			Send("AotdEmail", "the Article of the Day", notifications);
		}
		public void SendCompanyUpdateNotifications(EmailNotification<CompanyUpdateEmailViewModel>[] notification) {
			Send("CompanyUpdateEmail", "company updates", notification);
		}
		public void SendEmailConfirmationNotification(EmailNotification<ConfirmationEmailViewModel> notification) {
			Send("ConfirmationEmail", null, notification);
		}
		public void SendFollowerDigestNotifications(EmailNotification<FollowerViewModel[]>[] notifications) {
			Send("FollowerDigestEmail", "a digest of your new followers", notifications);
		}
		public void SendFollowerNotification(EmailNotification<FollowerViewModel> notifications) {
			Send("FollowerEmail", "new follower notifications", notifications);
		}
		public void SendLoopbackDigestNotifications(EmailNotification<CommentViewModel[]>[] notifications) {
			Send("LoopbackDigestEmail", "a digest of comments on articles you've read", notifications);
		}
		public void SendLoopbackNotifications(EmailNotification<CommentViewModel>[] notifications) {
			Send("LoopbackEmail", "comments on articles you've read", notifications);
		}
		public void SendPasswordResetNotification(EmailNotification<PasswordResetEmailViewModel> notification) {
			Send("PasswordResetEmail", null, notification);
		}
		public void SendPostDigestNotifications(EmailNotification<PostViewModel[]>[] notifications) {
			Send("PostDigestEmail", "a digest of posts from people you follow", notifications);
		}
		public void SendPostNotifications(EmailNotification<PostEmailViewModel>[] notifications) {
			Send("PostEmail", "posts from people you follow", notifications);
		}
		public void SendReplyDigestNotifications(EmailNotification<CommentViewModel[]>[] notifications) {
			Send("ReplyDigestEmail", "a digest of replies to your comments", notifications);
		}
		public void SendReplyNotification(EmailNotification<CommentViewModel> notification) {
			Send("ReplyEmail", "replies to your comments", notification);
		}
		public void SendWelcomeNotification(EmailNotification<ConfirmationEmailViewModel> notification) {
			Send("WelcomeEmail", null, notification);
		}
	}
}