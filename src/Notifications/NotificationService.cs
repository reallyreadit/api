using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Encryption;
using api.Messaging;
using api.Messaging.Views;
using api.Messaging.Views.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Notifications {
	public class NotificationService {
		private readonly ApnsService apnsService;
		private readonly DatabaseOptions databaseOptions;
		private readonly TokenizationOptions tokenizationOptions;
		private readonly ServiceEndpointsOptions endpoints;
		private readonly EmailService emailService;
		private readonly ObfuscationService obfuscation;
		private readonly ILogger<NotificationService> logger;
		public NotificationService(
			ApnsService apnsService,
			IOptions<DatabaseOptions> databaseOptions,
			IOptions<TokenizationOptions> tokenizationOptions,
			IOptions<ServiceEndpointsOptions> endpoints,
			EmailService emailService,
			ObfuscationService obfuscation,
			ILogger<NotificationService> logger
		) {
			this.apnsService = apnsService;
			this.databaseOptions = databaseOptions.Value;
			this.tokenizationOptions = tokenizationOptions.Value;
			this.endpoints = endpoints.Value;
			this.emailService = emailService;
			this.obfuscation = obfuscation;
			this.logger = logger;
		}
		private async Task ClearAlert(
			NpgsqlConnection db,
			long userAccountId,
			long receiptId
		) {
			var receipt = await db.ClearAlert(receiptId);
			if (receipt != null) {
				await SendPushClearNotifications(
					db: db,
					userAccountId: userAccountId,
					clearedReceiptIds: receiptId
				);
			}
		}
		private ApnsNotification CreateApnsNotification(
			ApnsAlert alert,
			NotificationAlertDispatch dispatch,
			Uri url,
			string category = null
		) => (
			new ApnsNotification(
				receiptId: dispatch.ReceiptId,
				payload: new ApnsPayload(
					applePayload: new ApnsApplePayload(
						alert: dispatch.ViaPush ? alert : null,
						badge: dispatch.GetTotalBadgeCount(),
						category: category
					),
					alertStatus: dispatch,
					url: url
				),
				tokens: dispatch.PushDeviceTokens
			)
		);
		private Uri CreateArticleTrackingUrl(INotificationDispatch dispatch, NotificationChannel channel, long articleId) => (
			CreateTrackingUrl(
				dispatch: dispatch,
				channel: channel,
				action: NotificationAction.View,
				resource: ViewActionResource.Article,
				resourceId: articleId
			)
		);
		private Uri CreateCommentTrackingUrl(INotificationDispatch dispatch, NotificationChannel channel, long commentId) => (
			CreateTrackingUrl(
				dispatch: dispatch,
				channel: channel,
				action: NotificationAction.View,
				resource: ViewActionResource.Comment,
				resourceId: commentId
			)
		);
		private Uri CreateCommentsTrackingUrl(INotificationDispatch dispatch, NotificationChannel channel, long articleId) => (
			CreateTrackingUrl(
				dispatch: dispatch,
				channel: channel,
				action: NotificationAction.View,
				resource: ViewActionResource.Comments,
				resourceId: articleId
			)
		);
		private Uri CreateEmailConfirmationUrl(long emailConfirmationId) {
			var token = WebUtility.UrlEncode(StringEncryption.Encrypt(emailConfirmationId.ToString(), tokenizationOptions.EncryptionKey));
			return new Uri(endpoints.WebServer.CreateUrl($"/confirmEmail?token={token}"));
		}
		private Uri CreateEmailOpenTrackingUrl(INotificationDispatch dispatch) => (
			CreateTrackingUrl(
				dispatch: dispatch,
				channel: NotificationChannel.Email,
				action: NotificationAction.Open
			)
		);
		private string CreateEmailReplyAddress(INotificationDispatch dispatch) {
			var replyToken = new NotificationToken(
					receiptId: dispatch.ReceiptId
				)
				.CreateTokenString(tokenizationOptions.EncryptionKey);
			return $"reply+{replyToken}@api.readup.com";
		}
		private Uri CreateFollowerTrackingUrl(INotificationDispatch dispatch, NotificationChannel channel, long followingId) => (
			CreateTrackingUrl(
				dispatch: dispatch,
				channel: channel,
				action: NotificationAction.View,
				resource: ViewActionResource.Follower,
				resourceId: followingId
			)
		);
		private async Task CreateOpenInteraction(
			NpgsqlConnection db,
			long receiptId
		) {
			try {
				await db.CreateNotificationInteraction(
					receiptId: receiptId,
					channel: NotificationChannel.Email,
					action: NotificationAction.Open
				);
			} catch (NpgsqlException ex) when (
				String.Equals(ex.Data["ConstraintName"], "notification_interaction_unique_open")
			) {
				// swallow duplicate open exception
			}
		}
		private Uri CreatePasswordResetUrl(long resetRequestId) {
			var token = WebUtility.UrlEncode(StringEncryption.Encrypt(resetRequestId.ToString(), tokenizationOptions.EncryptionKey));
			return new Uri(endpoints.WebServer.CreateUrl($"/resetPassword?token={token}"));
		}
		private Uri CreatePostTrackingUrl(INotificationDispatch dispatch, NotificationChannel channel, long? commentId, long? silentPostId) {
			if (
				(!commentId.HasValue && !silentPostId.HasValue) ||
				(commentId.HasValue && silentPostId.HasValue)
			) {
				throw new ArgumentException("Post must have only commentId or silentPostId");
			}
			if (commentId.HasValue) {
				return CreateTrackingUrl(
					dispatch: dispatch,
					channel: channel,
					action: NotificationAction.View,
					resource: ViewActionResource.CommentPost,
					resourceId: commentId
				);
			}
			return CreateTrackingUrl(
				dispatch: dispatch,
				channel: channel,
				action: NotificationAction.View,
				resource: ViewActionResource.SilentPost,
				resourceId: silentPostId
			);
		}
		private Uri CreateTrackingUrl(
			INotificationDispatch dispatch,
			NotificationChannel channel,
			NotificationAction action,
			ViewActionResource? resource = null,
			long? resourceId = null
		) {
			var token = new NotificationToken(
					receiptId: dispatch.ReceiptId,
					channel: channel,
					action: action,
					viewActionResource: resource,
					viewActionResourceId: resourceId
				)
				.CreateTokenString(tokenizationOptions.EncryptionKey);
			return new Uri(endpoints.ApiServer.CreateUrl($"/Notifications/{token}"));
		}
		private async Task CreateViewInteraction(
			NpgsqlConnection db,
			Notification notification,
			NotificationChannel channel,
			string url
		) {
			try {
				await db.CreateNotificationInteraction(
					receiptId: notification.ReceiptId,
					channel: channel,
					action: NotificationAction.View,
					url: url
				);
			} catch (NpgsqlException ex) when (
				String.Equals(ex.Data["ConstraintName"], "notification_interaction_unique_view")
			) {
				// swallow duplicate view exception
			}
			switch (notification.EventType) {
				case NotificationEventType.Aotd:
				case NotificationEventType.Follower:
				case NotificationEventType.Loopback:
				case NotificationEventType.Post:
				case NotificationEventType.Reply:
					if (!notification.DateAlertCleared.HasValue) {
						await ClearAlert(
							db: db,
							userAccountId: notification.UserAccountId,
							receiptId: notification.ReceiptId
						);
					}
					break;
			}
		}
		private async Task<Uri> CreateViewInteraction(
			NpgsqlConnection db,
			Notification notification,
			NotificationChannel channel,
			ViewActionResource resource,
			long resourceId
		) {
			var url = await CreateViewUrl(db, resource, resourceId);
			await CreateViewInteraction(db, notification, channel, url.ToString());
			return url;
		}
		private Uri CreateViewUrl(string path) => (
			new Uri(endpoints.WebServer.CreateUrl(path))
		);
		private async Task<Uri> CreateViewUrl(
			NpgsqlConnection db,
			ViewActionResource resource,
			long resourceId
		) {
			string path;
			switch (resource) {
				case ViewActionResource.Article:
				case ViewActionResource.Comments:
					var pathRoot = (
						resource == ViewActionResource.Article ?
							"read" :
							"comments"
					);
					var slugParts = (
							await db.GetArticle(
								articleId: resourceId
							)
						)
						.Slug.Split('_');
					path = $"/{pathRoot}/{slugParts[0]}/{slugParts[1]}";
					break;
				case ViewActionResource.Comment:
					return CreateViewUrlForComment(await db.GetComment(resourceId));
				case ViewActionResource.CommentPost:
					path = $"/following/comment/{obfuscation.Encode(resourceId)}";
					break;
				case ViewActionResource.SilentPost:
					path = $"/following/post/{obfuscation.Encode(resourceId)}";
					break;
				case ViewActionResource.Follower:
					var following = await db.GetFollowing(
						followingId: resourceId
					);
					return CreateViewUrlForFollower(
						followeeUserName: (await db.GetUserAccountById(following.FolloweeUserAccountId)).Name,
						followerUserName: (await db.GetUserAccountById(following.FollowerUserAccountId)).Name
					);
				default:
					throw new ArgumentException($"Unexpected value for {nameof(resource)}");
			}
			return new Uri(endpoints.WebServer.CreateUrl(path));
		}
		private Uri CreateViewUrlForComment(
			Comment comment
		) {
			var slugParts = comment.ArticleSlug.Split('_');
			return CreateViewUrl(path: $"/comments/{slugParts[0]}/{slugParts[1]}/{obfuscation.Encode(comment.Id)}");
		}
		private Uri CreateViewUrlForFollower(
			string followeeUserName,
			string followerUserName
		) => (
			CreateViewUrl(path: $"/@{followeeUserName}?followers&user={followerUserName}")
		);
		private async Task SendPushClearNotifications(
			NpgsqlConnection db,
			long userAccountId,
			params long[] clearedReceiptIds
		) {
			var pushDevices = await db.GetRegisteredPushDevices(userAccountId);
			if (pushDevices.Any()) {
				var userAccount = await db.GetUserAccountById(userAccountId);
				await apnsService.Send(
					new ApnsNotification(
						payload: new ApnsPayload(
							applePayload: new ApnsApplePayload(
								badge: userAccount.GetTotalBadgeCount()
							),
							alertStatus: userAccount,
							clearedNotificationIds: clearedReceiptIds
								.Select(id => obfuscation.Encode(id))
								.ToArray()
						),
						tokens: pushDevices
							.Select(device => device.Token)
							.ToArray()
					)
				);
			}
		}
		public async Task ClearAlerts(
			long userAccountId,
			params NotificationEventType[] types
		) {
			var clearedReceipts = new List<NotificationReceipt>();
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				foreach (var type in types) {
					if (type == NotificationEventType.Aotd) {
						clearedReceipts.Add(await db.ClearAotdAlert(userAccountId));
					} else {
						clearedReceipts.AddRange(await db.ClearAllAlerts(type, userAccountId));
					}
				}
				if (clearedReceipts.Any()) {
					await SendPushClearNotifications(
						db: db,
						userAccountId: userAccountId,
						clearedReceiptIds: clearedReceipts
							.Select(receipt => receipt.Id)
							.ToArray()
					);
				}
			}
		}
		public async Task CreateAotdDigestNotifications() {
			IEnumerable<NotificationEmailDispatch> dispatches;
			IEnumerable<Article> articles;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatches = await db.CreateAotdDigestNotifications();
				articles = await db.GetAotds(7);
			}
			if (dispatches.Any()) {
				await emailService.SendAotdDigestNotifications(
					dispatches
						.Select(
							dispatch => new EmailNotification<ArticleViewModel[]>(
								userId: dispatch.UserAccountId,
								to: new EmailMailbox(
									name: dispatch.UserName,
									address: dispatch.EmailAddress
								),
								subject: "The AOTD Weekly",
								openUrl: CreateEmailOpenTrackingUrl(dispatch),
								content: articles
									.OrderByDescending(article => article.AotdTimestamp)
									.Select(
										article => new ArticleViewModel(
											article: article,
											readArticleUrl: CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, article.Id),
											viewCommentsUrl: CreateCommentsTrackingUrl(dispatch, NotificationChannel.Email, article.Id)
										)
									)
									.ToArray()
							)
						)
						.ToArray()
				);
			}
		}
		public async Task CreateAotdNotifications(
			Article article
		) {
			IEnumerable<NotificationAlertDispatch> dispatches;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatches = await db.CreateAotdNotifications(
					articleId: article.Id
				);
			}
			if (dispatches.Any(dispatch => dispatch.PushDeviceTokens.Any())) {
				var alert = new ApnsAlert(
					title: "Article of the Day",
					subtitle: article.Title,
					body: article.GetFormattedByline()
				);
				await apnsService.Send(
					dispatches
						.Where(dispatch => dispatch.PushDeviceTokens.Any())
						.Select(
							dispatch => CreateApnsNotification(
								alert: alert,
								dispatch: dispatch,
								url: CreateArticleTrackingUrl(dispatch, NotificationChannel.Push, article.Id)
							)
						)
						.ToArray()
				);
			}
			if (dispatches.Any(dispatch => dispatch.ViaEmail)) {
				var learnMoreUrl = new Uri("https://blog.readup.com/?");
				await emailService.SendAotdNotifications(
					dispatches
						.Where(dispatch => dispatch.ViaEmail)
						.Select(
							dispatch => new EmailNotification<AotdEmailViewModel>(
								userId: dispatch.UserAccountId,
								to: new EmailMailbox(
									name: dispatch.UserName,
									address: dispatch.EmailAddress
								),
								subject: "AOTD: " + article.Title,
								openUrl: CreateEmailOpenTrackingUrl(dispatch),
								content: new AotdEmailViewModel(
									article: article,
									readArticleUrl: CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, article.Id),
									viewCommentsUrl: CreateCommentsTrackingUrl(dispatch, NotificationChannel.Email, article.Id),
									learnMoreUrl: learnMoreUrl
								)
							)
						)
						.ToArray()
				);
			}
		}
		public async Task CreateCompanyUpdateNotifications(
			long authorId,
			string subject,
			string body,
			string testEmailAddress
		) {
			var links = new Dictionary<string, (string Text, long ArticleId)>();
			var linkMatches = Regex
				.Matches(body, @"\[([^]]+)\]\(([^\)]+)\)", RegexOptions.Multiline)
				.Where(match => match.Success);
			IEnumerable<NotificationEmailDispatch> dispatches;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				foreach (var match in linkMatches) {
					var article = db.FindArticle(match.Groups[2].Value.Split(':')?[1], null);
					if (article == null) {
						throw new ArgumentException("Invalid article slug");
					}
					links.Add(match.Groups[0].Value, (Text: match.Groups[1].Value, ArticleId: article.Id));
				}
				if (testEmailAddress != null) {
					dispatches = new[] {
						new NotificationEmailDispatch() {
							ReceiptId = 0,
							UserAccountId = 0,
							UserName = "Test User",
							EmailAddress = testEmailAddress
						}
					};
				} else {
					dispatches = await db.CreateCompanyUpdateNotifications(authorId, subject, body);
				}
			}
			if (dispatches.Any()) {
				await emailService.SendCompanyUpdateNotifications(
					dispatches
						.Select(
							dispatch => {
								var html = body;
								foreach (var link in links) {
									var href = CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, link.Value.ArticleId);
									html = html.Replace(link.Key, $"<a href=\"{href}\">{link.Value.Text}</a>");
								}
								return new EmailNotification<CompanyUpdateEmailViewModel>(
									userId: dispatch.UserAccountId,
									to: new EmailMailbox(
										name: dispatch.UserName,
										address: dispatch.EmailAddress
									),
									subject: subject,
									openUrl: CreateEmailOpenTrackingUrl(dispatch),
									content: new CompanyUpdateEmailViewModel(
										html: html
									)
								);
							}
						)
						.ToArray()
				);
			}
		}
		public async Task CreateEmailConfirmationNotification(
			long userAccountId,
			long? confirmationId = null
		) {
			NotificationEmailDispatch dispatch;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				if (!confirmationId.HasValue) {
					confirmationId = db.CreateEmailConfirmation(userAccountId).Id;
				}
				dispatch = await db.CreateTransactionalNotification(
					userAccountId: userAccountId,
					eventType: NotificationEventType.EmailConfirmation,
					emailConfirmationId: confirmationId,
					passwordResetRequestId: null
				);
			}
			await emailService.SendEmailConfirmationNotification(
				new EmailNotification<ConfirmationEmailViewModel>(
					userId: dispatch.UserAccountId,
					to: new EmailMailbox(
						name: dispatch.UserName,
						address: dispatch.EmailAddress
					),
					subject: $"Email Confirmation",
					openUrl: CreateEmailOpenTrackingUrl(dispatch),
					content: new ConfirmationEmailViewModel(
						emailConfirmationUrl: CreateEmailConfirmationUrl(confirmationId.Value)
					)
				)
			);
		}
		public async Task CreateFollowerDigestNotifications(
			NotificationEventFrequency frequency
		) {
			IEnumerable<NotificationDigestDispatch<NotificationDigestFollower>> dispatches;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatches = await db.CreateFollowerDigestNotifications(frequency);
			}
			if (dispatches.Any()) {
				await emailService.SendFollowerDigestNotifications(
					dispatches
						.Select(
							dispatch => new EmailNotification<FollowerViewModel[]>(
								userId: dispatch.UserAccountId,
								to: new EmailMailbox(
									name: dispatch.UserName,
									address: dispatch.EmailAddress
								),
								subject: $"[{frequency.ToString()} digest] Your new followers",
								openUrl: CreateEmailOpenTrackingUrl(dispatch),
								content: dispatch.Items
									.OrderByDescending(follower => follower.DateFollowed)
									.Select(
										follower => new FollowerViewModel(
											userName: follower.UserName,
											viewProfileUrl: CreateFollowerTrackingUrl(dispatch, NotificationChannel.Email, follower.FollowingId)
										)
									)
									.ToArray()
							)
						)
						.ToArray()
				);
			}
		}
		public async Task CreateFollowerNotification(
			Following following
		) {
			NotificationAlertDispatch dispatch;
			string followerUserName;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatch = await db.CreateFollowerNotification(
					followingId: following.Id,
					followerId: following.FollowerUserAccountId,
					followeeId: following.FolloweeUserAccountId
				);
				if (
					(dispatch?.ViaEmail ?? false) ||
					(dispatch?.PushDeviceTokens.Any() ?? false)
				 ) {
					followerUserName = (await db.GetUserAccountById(following.FollowerUserAccountId)).Name;
				} else {
					followerUserName = null;
				}
			}
			if (dispatch?.ViaEmail ?? false) {
				await emailService.SendFollowerNotification(
					new EmailNotification<FollowerViewModel>(
						userId: dispatch.UserAccountId,
						to: new EmailMailbox(
							name: dispatch.UserName,
							address: dispatch.EmailAddress
						),
						subject: $"{followerUserName} is now following you",
						openUrl: CreateEmailOpenTrackingUrl(dispatch),
						content: new FollowerViewModel(
							userName: followerUserName,
							viewProfileUrl: CreateFollowerTrackingUrl(dispatch, NotificationChannel.Email, following.Id)
						)
					)
				);
			}
			if (dispatch?.PushDeviceTokens.Any() ?? false) {
				await apnsService.Send(
					CreateApnsNotification(
						alert: new ApnsAlert(
							title: $"{followerUserName} is now following you"
						),
						dispatch: dispatch,
						url: CreateFollowerTrackingUrl(dispatch, NotificationChannel.Push, following.Id)
					)
				);
			}
		}
		public async Task CreateLoopbackDigestNotifications(
			NotificationEventFrequency frequency
		) {
			IEnumerable<NotificationDigestDispatch<NotificationDigestComment>> dispatches;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatches = await db.CreateLoopbackDigestNotifications(frequency);
			}
			if (dispatches.Any()) {
				await emailService.SendLoopbackDigestNotifications(
					dispatches
						.Select(
							dispatch => new EmailNotification<CommentViewModel[]>(
								userId: dispatch.UserAccountId,
								to: new EmailMailbox(
									name: dispatch.UserName,
									address: dispatch.EmailAddress
								),
								subject: $"[{frequency.ToString()} digest] Comments on articles you've read",
								openUrl: CreateEmailOpenTrackingUrl(dispatch),
								content: dispatch.Items
									.OrderByDescending(comment => comment.DateCreated)
									.Select(
										comment => new CommentViewModel(
											author: comment.Author,
											article: comment.ArticleTitle,
											text: comment.Text,
											readArticleUrl: CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, comment.ArticleId),
											viewCommentUrl: CreateCommentTrackingUrl(dispatch, NotificationChannel.Email, comment.Id)
										)
									)
									.ToArray()
							)
						)
						.ToArray()
				);
			}
		}
		public async Task CreateLoopbackNotifications(
			Comment comment
		) {
			IEnumerable<NotificationAlertDispatch> dispatches;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatches = await db.CreateLoopbackNotifications(
					articleId: comment.ArticleId,
					commentId: comment.Id,
					commentAuthorId: comment.UserAccountId
				);
			}
			if (dispatches.Any(dispatch => dispatch.PushDeviceTokens.Any())) {
				var alert = new ApnsAlert(
					title: $"{comment.UserAccount} commented on {comment.ArticleTitle}",
					body: comment.Text
				);
				await apnsService.Send(
					dispatches
						.Where(dispatch => dispatch.PushDeviceTokens.Any())
						.Select(
							dispatch => CreateApnsNotification(
								alert: alert,
								dispatch: dispatch,
								url: CreateCommentTrackingUrl(dispatch, NotificationChannel.Push, comment.Id),
								category: "replyable"
							)
						)
						.ToArray()
				);
			}
			if (dispatches.Any(dispatch => dispatch.ViaEmail)) {
				await emailService.SendLoopbackNotifications(
					dispatches
						.Where(dispatch => dispatch.ViaEmail)
						.Select(
							dispatch => new EmailNotification<CommentViewModel>(
								userId: dispatch.UserAccountId,
								replyTo: new EmailMailbox(
									name: comment.UserAccount,
									address: CreateEmailReplyAddress(dispatch)
								),
								to: new EmailMailbox(
									name: dispatch.UserName,
									address: dispatch.EmailAddress
								),
								subject: $"{comment.UserAccount} commented on {comment.ArticleTitle}",
								openUrl: CreateEmailOpenTrackingUrl(dispatch),
								content: new CommentViewModel(
									author: comment.UserAccount,
									article: comment.ArticleTitle,
									text: comment.Text,
									readArticleUrl: CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, comment.ArticleId),
									viewCommentUrl: CreateCommentTrackingUrl(dispatch, NotificationChannel.Email, comment.Id)
								)
							)
						)
						.ToArray()
				);
			}
		}
		public async Task CreatePasswordResetNotification(
			long userAccountId
		) {
			PasswordResetRequest resetRequest;
			NotificationEmailDispatch dispatch;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				resetRequest = db.CreatePasswordResetRequest(userAccountId);
				dispatch = await db.CreateTransactionalNotification(
					userAccountId: userAccountId,
					eventType: NotificationEventType.PasswordReset,
					emailConfirmationId: null,
					passwordResetRequestId: resetRequest.Id
				);
			}
			await emailService.SendPasswordResetNotification(
				new EmailNotification<PasswordResetEmailViewModel>(
					userId: dispatch.UserAccountId,
					to: new EmailMailbox(
						name: dispatch.UserName,
						address: dispatch.EmailAddress
					),
					subject: $"Password Reset",
					openUrl: CreateEmailOpenTrackingUrl(dispatch),
					content: new PasswordResetEmailViewModel(
						passwordResetUrl: CreatePasswordResetUrl(resetRequest.Id)
					)
				)
			);
		}
		public async Task CreatePostDigestNotifications(
			NotificationEventFrequency frequency
		) {
			IEnumerable<NotificationDigestDispatch<NotificationDigestPost>> dispatches;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatches = await db.CreatePostDigestNotifications(frequency);
			}
			if (dispatches.Any()) {
				await emailService.SendPostDigestNotifications(
					dispatches
						.Select(
							dispatch => new EmailNotification<PostViewModel[]>(
								userId: dispatch.UserAccountId,
								to: new EmailMailbox(
									name: dispatch.UserName,
									address: dispatch.EmailAddress
								),
								subject: $"[{frequency.ToString()} digest] Posts from people you follow",
								openUrl: CreateEmailOpenTrackingUrl(dispatch),
								content: dispatch.Items
									.OrderByDescending(post => post.DateCreated)
									.Select(
										post => new PostViewModel(
											author: post.Author,
											article: post.ArticleTitle,
											text: post.CommentText,
											readArticleUrl: CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, post.ArticleId),
											viewPostUrl: CreatePostTrackingUrl(dispatch, NotificationChannel.Email, post.CommentId, post.SilentPostId)
										)
									)
									.ToArray()
							)
						)
						.ToArray()
				);
			}
		}
		public async Task CreatePostNotifications(
			long userAccountId,
			string userName,
			long articleId,
			string articleTitle,
			long? commentId,
			string commentText,
			long? silentPostId
		) {
			IEnumerable<NotificationPostAlertDispatch> dispatches;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatches = await db.CreatePostNotifications(
					articleId: articleId,
					posterId: userAccountId,
					commentId: commentId,
					silentPostId: silentPostId
				);
			}
			if (dispatches.Any(dispatch => dispatch.PushDeviceTokens.Any())) {
				var alert = new ApnsAlert(
					title: $"{userName} posted {articleTitle}",
					body: commentText
				);
				await apnsService.Send(
					dispatches
						.Where(dispatch => dispatch.PushDeviceTokens.Any())
						.Select(
							dispatch => CreateApnsNotification(
								alert: alert,
								dispatch: dispatch,
								url: CreatePostTrackingUrl(dispatch, NotificationChannel.Push, commentId, silentPostId),
								category: dispatch.IsReplyable ? "replyable" : null
							)
						)
						.ToArray()
				);
			}
			if (dispatches.Any(dispatch => dispatch.ViaEmail)) {
				await emailService.SendPostNotifications(
					dispatches
						.Where(dispatch => dispatch.ViaEmail)
						.Select(
							dispatch => new EmailNotification<PostEmailViewModel>(
								userId: dispatch.UserAccountId,
								replyTo: dispatch.IsReplyable ?
									new EmailMailbox(
										name: userName,
										address: CreateEmailReplyAddress(dispatch)
									) :
									null,
								to: new EmailMailbox(
									name: dispatch.UserName,
									address: dispatch.EmailAddress
								),
								subject: $"{userName} posted {articleTitle}",
								openUrl: CreateEmailOpenTrackingUrl(dispatch),
								content: new PostEmailViewModel(
									post: new PostViewModel(
										author: userName,
										article: articleTitle,
										text: commentText,
										readArticleUrl: CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, articleId),
										viewPostUrl: CreatePostTrackingUrl(dispatch, NotificationChannel.Email, commentId, silentPostId)
									),
									isReplyable: dispatch.IsReplyable
								)
							)
						)
						.ToArray()
				);
			}
		}
		public async Task CreateReplyDigestNotifications(
			NotificationEventFrequency frequency
		) {
			IEnumerable<NotificationDigestDispatch<NotificationDigestComment>> dispatches;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatches = await db.CreateReplyDigestNotifications(frequency);
			}
			if (dispatches.Any()) {
				await emailService.SendReplyDigestNotifications(
					dispatches
						.Select(
							dispatch => new EmailNotification<CommentViewModel[]>(
								userId: dispatch.UserAccountId,
								to: new EmailMailbox(
									name: dispatch.UserName,
									address: dispatch.EmailAddress
								),
								subject: $"[{frequency.ToString()} digest] Replies to your comments",
								openUrl: CreateEmailOpenTrackingUrl(dispatch),
								content: dispatch.Items
									.OrderByDescending(comment => comment.DateCreated)
									.Select(
										comment => new CommentViewModel(
											author: comment.Author,
											article: comment.ArticleTitle,
											text: comment.Text,
											readArticleUrl: CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, comment.ArticleId),
											viewCommentUrl: CreateCommentTrackingUrl(dispatch, NotificationChannel.Email, comment.Id)
										)
									)
									.ToArray()
							)
						)
						.ToArray()
				);
			}
		}
		public async Task CreateReplyNotification(
			Comment comment
		) {
			NotificationAlertDispatch dispatch;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatch = await db.CreateReplyNotification(
					replyId: comment.Id,
					replyAuthorId: comment.UserAccountId,
					parentId: comment.ParentCommentId.Value
				);
			}
			if (dispatch?.ViaEmail ?? false) {
				await emailService.SendReplyNotification(
					new EmailNotification<CommentViewModel>(
						userId: dispatch.UserAccountId,
						replyTo: new EmailMailbox(
							name: comment.UserAccount,
							address: CreateEmailReplyAddress(dispatch)
						),
						to: new EmailMailbox(
							name: dispatch.UserName,
							address: dispatch.EmailAddress
						),
						subject: $"{comment.UserAccount} replied to your comment",
						openUrl: CreateEmailOpenTrackingUrl(dispatch),
						content: new CommentViewModel(
							author: comment.UserAccount,
							article: comment.ArticleTitle,
							text: comment.Text,
							readArticleUrl: CreateArticleTrackingUrl(dispatch, NotificationChannel.Email, comment.ArticleId),
							viewCommentUrl: CreateCommentTrackingUrl(dispatch, NotificationChannel.Email, comment.Id)
						)
					)
				);
			}
			if (dispatch?.PushDeviceTokens.Any() ?? false) {
				await apnsService.Send(
					CreateApnsNotification(
						alert: new ApnsAlert(
							title: "Re: " + comment.ArticleTitle,
							subtitle: comment.UserAccount,
							body: comment.Text
						),
						dispatch: dispatch,
						url: CreateCommentTrackingUrl(dispatch, NotificationChannel.Push, comment.Id),
						category: "replyable"
					)
				);
			}
		}
		public async Task CreateWelcomeNotification(
			long userAccountId
		) {
			EmailConfirmation emailConfirmation;
			NotificationEmailDispatch dispatch;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				emailConfirmation = db.CreateEmailConfirmation(userAccountId);
				dispatch = await db.CreateTransactionalNotification(
					userAccountId: userAccountId,
					eventType: NotificationEventType.Welcome,
					emailConfirmationId: emailConfirmation.Id,
					passwordResetRequestId: null
				);
			}
			await emailService.SendWelcomeNotification(
				new EmailNotification<ConfirmationEmailViewModel>(
					userId: dispatch.UserAccountId,
					to: new EmailMailbox(
						name: dispatch.UserName,
						address: dispatch.EmailAddress
					),
					subject: $"Welcome to Readup",
					openUrl: CreateEmailOpenTrackingUrl(dispatch),
					content: new ConfirmationEmailViewModel(
						emailConfirmationUrl: CreateEmailConfirmationUrl(emailConfirmation.Id)
					)
				)
			);
		}
		public NotificationToken DecryptTokenString(
			string tokenString
		) => new NotificationToken(
			tokenString: tokenString,
			key: tokenizationOptions.EncryptionKey
		);
		public async Task LogAuthDenial(
			long userAccountId,
			string installationId,
			string deviceName
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				await db.CreateNotificationPushAuthDenial(userAccountId, installationId, deviceName);
			}
		}
		public async Task ProcessEmailReply(
			long userAccountId,
			long receiptId,
			long replyId
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				await db.CreateNotificationInteraction(
					receiptId: receiptId,
					channel: NotificationChannel.Email,
					action: NotificationAction.Reply,
					replyId: replyId
				);
				await ClearAlert(
					db: db,
					userAccountId: userAccountId,
					receiptId: receiptId
				);
			}
		}
		public async Task<( NotificationAction Action, Uri RedirectUrl )?> ProcessEmailRequest(
			string tokenString
		) {
			var token = DecryptTokenString(tokenString);
			if (token.Channel.HasValue && token.Action.HasValue) {
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					switch (token.Action) {
						case NotificationAction.Open:
							if (token.ReceiptId != 0) {
								await CreateOpenInteraction(
									db: db,
									receiptId: token.ReceiptId
								);
							}
							return (NotificationAction.Open, null);
						case NotificationAction.View:
							return (
								NotificationAction.View,
								token.ReceiptId != 0 ?
									await CreateViewInteraction(
										db: db,
										notification: await db.GetNotification(token.ReceiptId),
										channel: token.Channel.Value,
										resource: token.ViewActionResource.Value,
										resourceId: token.ViewActionResourceId.Value
									) :
									await CreateViewUrl(
										db: db,
										resource: token.ViewActionResource.Value,
										resourceId: token.ViewActionResourceId.Value
									)
							);
					}
				}
			}
			return null;
		}
		public async Task<Uri> ProcessExtensionRequest(
			long receiptId
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				var notification = await db.GetNotification(receiptId);
				ViewActionResource resource;
				long resourceId;
				switch (notification.EventType) {
					case NotificationEventType.Aotd:
						resource = ViewActionResource.Article;
						resourceId = notification.ArticleIds.Single();
						break;
					case NotificationEventType.Reply:
					case NotificationEventType.Loopback:
						resource = ViewActionResource.Comment;
						resourceId = notification.CommentIds.Single();
						break;
					case NotificationEventType.Post:
						if (notification.CommentIds.Any()) {
							resource = ViewActionResource.CommentPost;
							resourceId = notification.CommentIds.Single();
						} else {
							resource = ViewActionResource.SilentPost;
							resourceId = notification.SilentPostIds.Single();
						}
						break;
					case NotificationEventType.Follower:
						resource = ViewActionResource.Follower;
						resourceId = notification.FollowingIds.Single();
						break;
					default:
						throw new InvalidOperationException($"Unexpected value for {nameof(notification.EventType)}");
				}
				return await CreateViewInteraction(
					db: db,
					notification: notification,
					channel: NotificationChannel.Extension,
					resource: resource,
					resourceId: resourceId
				);
			}
		}
		public async Task ProcessPushReply(
			long userAccountId,
			long receiptId,
			long replyId
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				await db.CreateNotificationInteraction(
					receiptId: receiptId,
					channel: NotificationChannel.Push,
					action: NotificationAction.Reply,
					replyId: replyId
				);
				await ClearAlert(
					db: db,
					userAccountId: userAccountId,
					receiptId: receiptId
				);
			}
		}
		public async Task ProcessPushRequest(
			Notification notification,
			string url
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				await CreateViewInteraction(
					db: db,
					notification: notification,
					channel: NotificationChannel.Push,
					url: url
				);
			}
		}
		public async Task RegisterPushDevice(
			long userAccountId,
			string installationId,
			string name,
			string token
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				await db.RegisterNotificationPushDevice(userAccountId, installationId, name, token);
			}
		}
	}
}