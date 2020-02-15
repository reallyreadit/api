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
									logoUrl: serviceOpts.StaticContentServer.CreateUrl("/email/logo.png"),
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
				// the razor renderer doesn't work properly when rendered the background
				// after the request context has been disposed. not sure exactly what
				// is going out of scope but it happens in the call to RenderAsync
				taskQueue.QueueBackgroundWorkItem(
					async token => {
						await Send(messages.ToArray());
					}
				);
			}
		}
		protected abstract Task Send(params EmailMessage[] messages);
		public bool HasEmailAddressBounced(string emailAddress) => (
			this.bouncedAddresses.Value.Contains(NormalizeEmailAddress(emailAddress))
		);
		public async Task SendAotdDigestNotifications(EmailNotification<ArticleViewModel[]>[] notifications) {
			await Send("AotdDigestEmail", "the Article of the Day digest", notifications);
		}
		public async Task SendAotdNotifications(EmailNotification<AotdEmailViewModel>[] notifications) {
			await Send("AotdEmail", "the Article of the Day", notifications);
		}
		public async Task SendCompanyUpdateNotifications(EmailNotification<CompanyUpdateEmailViewModel>[] notification) {
			await Send("CompanyUpdateEmail", "company updates", notification);
		}
		public async Task SendEmailConfirmationNotification(EmailNotification<ConfirmationEmailViewModel> notification) {
			await Send("ConfirmationEmail", null, notification);
		}
		public async Task SendFollowerDigestNotifications(EmailNotification<FollowerViewModel[]>[] notifications) {
			await Send("FollowerDigestEmail", "a digest of your new followers", notifications);
		}
		public async Task SendFollowerNotification(EmailNotification<FollowerViewModel> notifications) {
			await Send("FollowerEmail", "new follower notifications", notifications);
		}
		public async Task SendLoopbackDigestNotifications(EmailNotification<CommentViewModel[]>[] notifications) {
			await Send("LoopbackDigestEmail", "a digest of comments on articles you've read", notifications);
		}
		public async Task SendLoopbackNotifications(EmailNotification<CommentViewModel>[] notifications) {
			await Send("LoopbackEmail", "comments on articles you've read", notifications);
		}
		public async Task SendPasswordResetNotification(EmailNotification<PasswordResetEmailViewModel> notification) {
			await Send("PasswordResetEmail", null, notification);
		}
		public async Task SendPostDigestNotifications(EmailNotification<PostViewModel[]>[] notifications) {
			await Send("PostDigestEmail", "a digest of posts from people you follow", notifications);
		}
		public async Task SendPostNotifications(EmailNotification<PostEmailViewModel>[] notifications) {
			await Send("PostEmail", "posts from people you follow", notifications);
		}
		public async Task SendReplyDigestNotifications(EmailNotification<CommentViewModel[]>[] notifications) {
			await Send("ReplyDigestEmail", "a digest of replies to your comments", notifications);
		}
		public async Task SendReplyNotification(EmailNotification<CommentViewModel> notification) {
			await Send("ReplyEmail", "replies to your comments", notification);
		}
		public async Task SendWelcomeNotification(EmailNotification<WelcomeEmailViewModel> notification) {
			await Send("WelcomeEmail", null, notification);
		}
	}
}