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
		private Uri CreateArticleEmailUrl(INotificationDispatch dispatch, long articleId) => (
			CreateEmailUrl(
				dispatch: dispatch,
				resource: EmailLinkResource.Article,
				resourceId: articleId
			)
		);
		private Uri CreateArticleUrl(string slug) {
			var slugParts = slug.Split('_');
			return new Uri(endpoints.WebServer.CreateUrl($"/read/{slugParts[0]}/{slugParts[1]}"));
		}
		private Uri CreateCommentEmailUrl(INotificationDispatch dispatch, long commentId) => (
			CreateEmailUrl(
				dispatch: dispatch,
				resource: EmailLinkResource.Comment,
				resourceId: commentId
			)
		);
		private Uri CreateCommentUrl(string slug, long commentId) {
			var slugParts = slug.Split('_');
			return new Uri(endpoints.WebServer.CreateUrl($"/comments/{slugParts[0]}/{slugParts[1]}/{obfuscation.Encode(commentId)}"));
		}
		private Uri CreateCommentsEmailUrl(INotificationDispatch dispatch, long articleId) => (
			CreateEmailUrl(
				dispatch: dispatch,
				resource: EmailLinkResource.Comments,
				resourceId: articleId
			)
		);
		private Uri CreateCommentsUrl(string slug) {
			var slugParts = slug.Split('_');
			return new Uri(endpoints.WebServer.CreateUrl($"/comments/{slugParts[0]}/{slugParts[1]}"));
		}
		private Uri CreateEmailConfirmationUrl(long emailConfirmationId) {
			var token = WebUtility.UrlEncode(StringEncryption.Encrypt(emailConfirmationId.ToString(), tokenizationOptions.EncryptionKey));
			return new Uri(endpoints.WebServer.CreateUrl($"/confirmEmail?token={token}"));
		}
		private Uri CreateEmailOpenUrl(INotificationDispatch dispatch) {
			var id = UrlSafeBase64.Encode(
				StringEncryption.Encrypt(
					text: dispatch.ReceiptId.ToString(),
					key: tokenizationOptions.EncryptionKey
				)
			);
			return new Uri(endpoints.ApiServer.CreateUrl($"/Email/Open/{id}"));
		}
		private string CreateEmailReplyAddress(INotificationDispatch dispatch) {
			var token = UrlSafeBase64.Encode(
				StringEncryption.Encrypt(
					text: dispatch.ReceiptId.ToString(),
					key: tokenizationOptions.EncryptionKey
				)
			);
			return $"reply+{token}@api.readup.com";
		}
		private Uri CreateFollowerEmailUrl(INotificationDispatch dispatch, long followingId) => (
			CreateEmailUrl(
				dispatch: dispatch,
				resource: EmailLinkResource.Follower,
				resourceId: followingId
			)
		);
		private Uri CreateFirstPosterEmailUrl(INotificationDispatch dispatch, long articleId) => (
			CreateEmailUrl(
				dispatch: dispatch,
				resource: EmailLinkResource.FirstPoster,
				resourceId: articleId
			)
		);
		private Uri CreateProfileUrl(string name) {
			return new Uri(endpoints.WebServer.CreateUrl($"/@{name}"));
		}
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
		private Uri CreatePostEmailUrl(INotificationDispatch dispatch, long? commentId, long? silentPostId) {
			if (
				(!commentId.HasValue && !silentPostId.HasValue) ||
				(commentId.HasValue && silentPostId.HasValue)
			) {
				throw new ArgumentException("Post must have only commentId or silentPostId");
			}
			if (commentId.HasValue) {
				return CreateEmailUrl(
					dispatch: dispatch,
					resource: EmailLinkResource.CommentPost,
					resourceId: commentId.Value
				);
			}
			return CreateEmailUrl(
				dispatch: dispatch,
				resource: EmailLinkResource.SilentPost,
				resourceId: silentPostId.Value
			);
		}
		private Uri CreatePostUrl(string authorName, long? commentId, long? silentPostId) {
			if (
				(!commentId.HasValue && !silentPostId.HasValue) ||
				(commentId.HasValue && silentPostId.HasValue)
			) {
				throw new ArgumentException("Post must have only commentId or silentPostId");
			}
			string path;
			if (commentId.HasValue) {
				path = $"/@{authorName}/comment/{obfuscation.Encode(commentId.Value)}";
			} else {
				path = $"/@{authorName}/post/{obfuscation.Encode(silentPostId.Value)}";
			}
			return new Uri(endpoints.WebServer.CreateUrl(path));
		}
		private Uri CreateEmailUrl(
			INotificationDispatch dispatch,
			EmailLinkResource resource,
			long resourceId
		) {
			var token = new EmailLinkToken(
					receiptId: dispatch.ReceiptId,
					resource: resource,
					resourceId: resourceId
				)
				.CreateTokenString(tokenizationOptions.EncryptionKey);
			return new Uri(endpoints.WebServer.CreateUrl($"/mailLink/{token}"));
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
			EmailLinkResource resource,
			long resourceId
		) {
			var url = await CreateViewUrl(db, resource, resourceId);
			await CreateViewInteraction(db, notification, channel, url.ToString());
			return url;
		}
		private async Task<Uri> CreateViewUrl(
			NpgsqlConnection db,
			EmailLinkResource resource,
			long resourceId
		) {
			switch (resource) {
				case EmailLinkResource.Article:
				case EmailLinkResource.Comments:
					var article = await db.GetArticle(resourceId);
					if (resource == EmailLinkResource.Article) {
						return CreateArticleUrl(article.Slug);
					} else {
						return CreateCommentsUrl(article.Slug);
					}
				case EmailLinkResource.Comment:
					var comment = await db.GetComment(resourceId);
					return CreateCommentUrl(comment.ArticleSlug, comment.Id);
				case EmailLinkResource.CommentPost:
					var postComment = await db.GetComment(resourceId);
					var commentPoster = await db.GetUserAccountById(postComment.UserAccountId);
					return CreatePostUrl(commentPoster.Name, postComment.Id, null);
				case EmailLinkResource.SilentPost:
					var silentPost = await db.GetSilentPost(resourceId);
					var silentPoster = await db.GetUserAccountById(silentPost.UserAccountId);
					return CreatePostUrl(silentPoster.Name, null, silentPost.Id);
				case EmailLinkResource.Follower:
					var follower = await db.GetUserAccountById(
						(await db.GetFollowing(resourceId)).FollowerUserAccountId
					);
					return CreateProfileUrl(follower.Name);
				case EmailLinkResource.FirstPoster:
					var firstPoster = await db.GetUserAccountByName(
						(await db.GetArticle(resourceId)).FirstPoster
					);
					return CreateProfileUrl(firstPoster.Name);
				default:
					throw new ArgumentException($"Unexpected value for {nameof(resource)}");
			}
		}
		private async Task SendPushClearNotifications(
			NpgsqlConnection db,
			long userAccountId,
			params long[] clearedReceiptIds
		) {
			var pushDevices = await db.GetRegisteredPushDevices(userAccountId);
			if (pushDevices.Any()) {
				var userAccount = await db.GetUserAccountById(userAccountId);
				apnsService.Send(
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
								openUrl: CreateEmailOpenUrl(dispatch),
								content: articles
									.OrderByDescending(article => article.AotdTimestamp)
									.Select(
										article => new ArticleViewModel(
											article: article,
											readArticleUrl: CreateArticleEmailUrl(dispatch, article.Id),
											viewCommentsUrl: CreateCommentsEmailUrl(dispatch, article.Id),
											viewFirstPosterProfileUrl: (
												article.FirstPoster != null ?
													CreateFirstPosterEmailUrl(dispatch, article.Id) :
													null
											)
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
				apnsService.Send(
					dispatches
						.Where(dispatch => dispatch.PushDeviceTokens.Any())
						.Select(
							dispatch => CreateApnsNotification(
								alert: alert,
								dispatch: dispatch,
								url: CreateArticleUrl(article.Slug)
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
								openUrl: CreateEmailOpenUrl(dispatch),
								content: new AotdEmailViewModel(
									article: article,
									readArticleUrl: CreateArticleEmailUrl(dispatch, article.Id),
									viewCommentsUrl: CreateCommentsEmailUrl(dispatch, article.Id),
									viewFirstPosterProfileUrl: (
										article.FirstPoster != null ?
											CreateFirstPosterEmailUrl(dispatch, article.Id) :
											null
									),
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
									var href = CreateArticleEmailUrl(dispatch, link.Value.ArticleId);
									html = html.Replace(link.Key, $"<a href=\"{href}\">{link.Value.Text}</a>");
								}
								return new EmailNotification<CompanyUpdateEmailViewModel>(
									userId: dispatch.UserAccountId,
									to: new EmailMailbox(
										name: dispatch.UserName,
										address: dispatch.EmailAddress
									),
									subject: subject,
									openUrl: CreateEmailOpenUrl(dispatch),
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
					openUrl: CreateEmailOpenUrl(dispatch),
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
								openUrl: CreateEmailOpenUrl(dispatch),
								content: dispatch.Items
									.OrderByDescending(follower => follower.DateFollowed)
									.Select(
										follower => new FollowerViewModel(
											userName: follower.UserName,
											viewProfileUrl: CreateFollowerEmailUrl(dispatch, follower.FollowingId)
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
						openUrl: CreateEmailOpenUrl(dispatch),
						content: new FollowerViewModel(
							userName: followerUserName,
							viewProfileUrl: CreateFollowerEmailUrl(dispatch, following.Id)
						)
					)
				);
			}
			if (dispatch?.PushDeviceTokens.Any() ?? false) {
				apnsService.Send(
					CreateApnsNotification(
						alert: new ApnsAlert(
							title: $"{followerUserName} is now following you"
						),
						dispatch: dispatch,
						url: CreateProfileUrl(followerUserName)
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
								openUrl: CreateEmailOpenUrl(dispatch),
								content: dispatch.Items
									.OrderByDescending(comment => comment.DateCreated)
									.Select(
										comment => new CommentViewModel(
											author: comment.Author,
											article: comment.ArticleTitle,
											text: comment.Text,
											readArticleUrl: CreateArticleEmailUrl(dispatch, comment.ArticleId),
											viewCommentUrl: CreateCommentEmailUrl(dispatch, comment.Id)
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
					body: WebUtility.HtmlDecode(comment.Text)
				);
				apnsService.Send(
					dispatches
						.Where(dispatch => dispatch.PushDeviceTokens.Any())
						.Select(
							dispatch => CreateApnsNotification(
								alert: alert,
								dispatch: dispatch,
								url: CreateCommentUrl(comment.ArticleSlug, comment.Id),
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
								openUrl: CreateEmailOpenUrl(dispatch),
								content: new CommentViewModel(
									author: comment.UserAccount,
									article: comment.ArticleTitle,
									text: comment.Text,
									readArticleUrl: CreateArticleEmailUrl(dispatch, comment.ArticleId),
									viewCommentUrl: CreateCommentEmailUrl(dispatch, comment.Id)
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
					openUrl: CreateEmailOpenUrl(dispatch),
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
								openUrl: CreateEmailOpenUrl(dispatch),
								content: dispatch.Items
									.OrderByDescending(post => post.DateCreated)
									.Select(
										post => new PostViewModel(
											author: post.Author,
											article: post.ArticleTitle,
											text: post.CommentText,
											readArticleUrl: CreateArticleEmailUrl(dispatch, post.ArticleId),
											viewPostUrl: CreatePostEmailUrl(dispatch, post.CommentId, post.SilentPostId)
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
					body: WebUtility.HtmlDecode(commentText)
				);
				apnsService.Send(
					dispatches
						.Where(dispatch => dispatch.PushDeviceTokens.Any())
						.Select(
							dispatch => CreateApnsNotification(
								alert: alert,
								dispatch: dispatch,
								url: CreatePostUrl(userName, commentId, silentPostId),
								category: commentId.HasValue && dispatch.HasRecipientReadArticle ? "replyable" : null
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
							dispatch => {
								var isReplyable = commentId.HasValue && dispatch.HasRecipientReadArticle;
								return new EmailNotification<PostEmailViewModel>(
									userId: dispatch.UserAccountId,
									replyTo: isReplyable ?
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
									openUrl: CreateEmailOpenUrl(dispatch),
									content: new PostEmailViewModel(
										post: new PostViewModel(
											author: userName,
											article: articleTitle,
											text: commentText,
											readArticleUrl: CreateArticleEmailUrl(dispatch, articleId),
											viewPostUrl: CreatePostEmailUrl(dispatch, commentId, silentPostId)
										),
										isReplyable: isReplyable
									)
								);
							}
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
								openUrl: CreateEmailOpenUrl(dispatch),
								content: dispatch.Items
									.OrderByDescending(comment => comment.DateCreated)
									.Select(
										comment => new CommentViewModel(
											author: comment.Author,
											article: comment.ArticleTitle,
											text: comment.Text,
											readArticleUrl: CreateArticleEmailUrl(dispatch, comment.ArticleId),
											viewCommentUrl: CreateCommentEmailUrl(dispatch, comment.Id)
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
						openUrl: CreateEmailOpenUrl(dispatch),
						content: new CommentViewModel(
							author: comment.UserAccount,
							article: comment.ArticleTitle,
							text: comment.Text,
							readArticleUrl: CreateArticleEmailUrl(dispatch, comment.ArticleId),
							viewCommentUrl: CreateCommentEmailUrl(dispatch, comment.Id)
						)
					)
				);
			}
			if (dispatch?.PushDeviceTokens.Any() ?? false) {
				apnsService.Send(
					CreateApnsNotification(
						alert: new ApnsAlert(
							title: $"{comment.UserAccount} replied to your comment",
							body: WebUtility.HtmlDecode(comment.Text)
						),
						dispatch: dispatch,
						url: CreateCommentUrl(comment.ArticleSlug, comment.Id),
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
					subject: $"Welcome to Readup!",
					openUrl: CreateEmailOpenUrl(dispatch),
					content: new ConfirmationEmailViewModel(
						emailConfirmationUrl: CreateEmailConfirmationUrl(emailConfirmation.Id)
					)
				)
			);
		}
		public long? GetReceiptIdFromEmailReplyAddress(string address) {
			var match = Regex.Match(
				input: EmailFormatting.ExtractEmailAddress(address),
				pattern: @"^reply\+([^@]+)@"
			);
			if (match.Success) {
				return Int64.Parse(
					StringEncryption.Decrypt(
						text: UrlSafeBase64.Decode(match.Groups[1].Value),
						key: tokenizationOptions.EncryptionKey
					)
				);
			}
			return null;
		}
		public async Task LogAuthDenial(
			long userAccountId,
			string installationId,
			string deviceName
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				await db.CreateNotificationPushAuthDenial(userAccountId, installationId, deviceName);
			}
		}
		public async Task<Uri> ProcessEmailLink(
			string tokenString
		) {
			var token = new EmailLinkToken(
				tokenString: tokenString,
				key: tokenizationOptions.EncryptionKey
			);
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				return (
					token.ReceiptId != 0 ?
						await CreateViewInteraction(
							db: db,
							notification: await db.GetNotification(token.ReceiptId),
							channel: NotificationChannel.Email,
							resource: token.Resource,
							resourceId: token.ResourceId
						) :
						await CreateViewUrl(
							db: db,
							resource: token.Resource,
							resourceId: token.ResourceId
						)
				);
			}
		}
		public async Task ProcessEmailOpen(
			string tokenString
		) {
			var receiptId = Int64.Parse(
				StringEncryption.Decrypt(
					text: UrlSafeBase64.Decode(tokenString),
					key: tokenizationOptions.EncryptionKey
				)
			);
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				if (receiptId != 0) {
					await CreateOpenInteraction(
						db: db,
						receiptId: receiptId
					);
				}
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
		public async Task<Uri> ProcessExtensionView(
			long receiptId
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				var notification = await db.GetNotification(receiptId);
				EmailLinkResource resource;
				long resourceId;
				switch (notification.EventType) {
					case NotificationEventType.Aotd:
						resource = EmailLinkResource.Article;
						resourceId = notification.ArticleIds.Single();
						break;
					case NotificationEventType.Reply:
					case NotificationEventType.Loopback:
						resource = EmailLinkResource.Comment;
						resourceId = notification.CommentIds.Single();
						break;
					case NotificationEventType.Post:
						if (notification.CommentIds.Any()) {
							resource = EmailLinkResource.CommentPost;
							resourceId = notification.CommentIds.Single();
						} else {
							resource = EmailLinkResource.SilentPost;
							resourceId = notification.SilentPostIds.Single();
						}
						break;
					case NotificationEventType.Follower:
						resource = EmailLinkResource.Follower;
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
		public async Task ProcessPushView(
			long receiptId,
			string url
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				var notification = await db.GetNotification(receiptId);
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