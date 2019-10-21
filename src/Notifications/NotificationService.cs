using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Encryption;
using api.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Notifications {
	public class NotificationService {
		private readonly ApnsService apnsService;
		private readonly DatabaseOptions databaseOptions;
		private readonly TokenizationOptions tokenizationOptions;
		private readonly IOptions<ServiceEndpointsOptions> endpoints;
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
			this.endpoints = endpoints;
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
		private async Task<Uri> CreateViewInteraction(
			NpgsqlConnection db,
			Notification notification,
			NotificationChannel channel,
			ViewActionResource resource,
			long resourceId
		) {
			string path;
			switch (resource) {
				case ViewActionResource.Article:
				case ViewActionResource.Comments:
				case ViewActionResource.Comment:
					long articleId;
					long? commentId;
					string pathRoot;
					if (resource == ViewActionResource.Comment) {
						articleId = (
								await db.GetComment(
									commentId: resourceId
								)
							)
							.ArticleId;
						commentId = resourceId;
						pathRoot = "comments";
					} else {
						articleId = resourceId;
						commentId = null;
						pathRoot = (
							resource == ViewActionResource.Article ?
								"read" :
								"comments"
						);
					}
					var slugParts = (
							await db.GetArticle(
								articleId: articleId
							)
						)
						.Slug.Split('_');
					path = $"/{pathRoot}/{slugParts[0]}/{slugParts[1]}";
					if (commentId.HasValue) {
						path += "/" + obfuscation.Encode(
							number: commentId.Value
						);
					}
					break;
				case ViewActionResource.CommentPost:
					path = $"/following/comment/{obfuscation.Encode(resourceId)}";
					break;
				case ViewActionResource.SilentPost:
					path = $"/following/post/{obfuscation.Encode(resourceId)}";
					break;
				case ViewActionResource.Follower:
					var userName = (
							await db.GetUserAccountById(
								userAccountId: (
									await db.GetFollowing(
										followingId: resourceId
									)
								)
								.FollowerUserAccountId
							)
						)
						.Name;
					path = $"/profile?followers&userName={userName}";
					break;
				default:
					throw new ArgumentException($"Unexpected value for {nameof(resource)}");
			}
			var url = endpoints.Value.WebServer.CreateUrl(path);
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
			await ClearAlertIfNeeded(
				db: db,
				notification: notification,
				action: NotificationAction.View
			);
			return new Uri(url);
		}
		private async Task SendPushAlertNotification(
			NotificationDispatch dispatch,
			ApnsAlert alert
		) {
			await apnsService.Send(
				new ApnsNotification(
					collapseId: dispatch.ReceiptId.ToString(),
					payload: new ApnsPayload(
						applePayload: new ApnsApplePayload(
							alert: dispatch.ViaPush ? alert : null,
							badge: dispatch.GetTotalBadgeCount()
						),
						alertStatus: dispatch
					),
					tokens: dispatch.PushDeviceTokens
				)
			);
		}
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
								.Select(id => id.ToString())
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
		public async Task CreateAotdNotifications(
			long articleId
		) {
			using (
				var db = new NpgsqlConnection(
					connectionString: databaseOptions.ConnectionString
				)
			) {
				foreach (
					var dispatch in await db.CreateAotdNotifications(
						articleId: articleId
					)
				) {
					if (dispatch.ViaEmail) {
						// send email
						Console.WriteLine("Send aotd via email");
					}
				}
			}
		}
		public async Task CreateFollowerNotification(
			Following following
		) {
			NotificationDispatch dispatch;
			string followerUserName;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatch = await db.CreateFollowerNotification(
					followingId: following.Id,
					followerId: following.FollowerUserAccountId,
					followeeId: following.FolloweeuserAccountId
				);
				followerUserName = (
						await db.GetUserAccountById(following.FollowerUserAccountId)
					)
					.Name;
			}
			if (dispatch?.ViaEmail ?? false) {
				// send email
				Console.WriteLine("Send follower via email");
			}
			if (dispatch?.PushDeviceTokens.Any() ?? false) {
				await SendPushAlertNotification(
					dispatch: dispatch,
					alert: new ApnsAlert(
						title: "New Follower",
						subtitle: "You have a new follower",
						body: $"{followerUserName} is now following you."
					)
				);
			}
		}
		public async Task CreateLoopbackNotifications(
			Comment comment
		) {
			using (
				var db = new NpgsqlConnection(
					connectionString: databaseOptions.ConnectionString
				)
			) {
				foreach (
					var dispatch in await db.CreateLoopbackNotifications(
						articleId: comment.ArticleId,
						commentId: comment.Id,
						commentAuthorId: comment.UserAccountId
					)
				) {
					if (dispatch.ViaEmail) {
						// send email
						Console.WriteLine("Send loopback via email");
					}
				}
			}
		}
		public async Task CreatePostNotifications(
			long userAccountId,
			long? commentId,
			long? silentPostId
		) {

			using (
				var db = new NpgsqlConnection(
					connectionString: databaseOptions.ConnectionString
				)
			) {
				foreach (
					var dispatch in await db.CreatePostNotifications(
						posterId: userAccountId,
						commentId: commentId,
						silentPostId: silentPostId
					)
				) {
					if (dispatch.ViaEmail) {
						// send email
						Console.WriteLine("Send post via email");
					}
				}
			}
		}
		public async Task CreateReplyNotification(
			Comment comment
		) {
			NotificationDispatch dispatch;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				dispatch = await db.CreateReplyNotification(
					replyId: comment.Id,
					replyAuthorId: comment.UserAccountId,
					parentId: comment.ParentCommentId.Value
				);
			}
			if (dispatch?.ViaEmail ?? false) {
				var replyToken = new NotificationToken(
						receiptId: dispatch.ReceiptId
					)
					.CreateTokenString(
						key: tokenizationOptions.EncryptionKey
					);
				await emailService.SendCommentReplyNotificationEmail(
					replyTo: new EmailMailbox(
						name: comment.UserAccount,
						address: $"reply+{replyToken}@api.readup.com"
					),
					dispatch: dispatch,
					openToken: new NotificationToken(
							receiptId: dispatch.ReceiptId,
							channel: NotificationChannel.Email,
							action: NotificationAction.Open
						)
						.CreateTokenString(
							key: tokenizationOptions.EncryptionKey
						),
					viewCommentToken: new NotificationToken(
							receiptId: dispatch.ReceiptId,
							channel: NotificationChannel.Email,
							action: NotificationAction.View,
							viewActionResource: ViewActionResource.Comment,
							viewActionResourceId: comment.Id
						)
						.CreateTokenString(
							key: tokenizationOptions.EncryptionKey
						),
					reply: comment
				);
			}
			if (dispatch?.PushDeviceTokens.Any() ?? false) {
				await SendPushAlertNotification(
					dispatch: dispatch,
					alert: new ApnsAlert(
						title: "Re: " + comment.ArticleTitle,
						subtitle: comment.UserAccount,
						body: comment.Text
					)
				);
			}
		}
		public NotificationToken DecryptTokenString(
			string tokenString
		) => new NotificationToken(
			tokenString: tokenString,
			key: tokenizationOptions.EncryptionKey
		);
		private async Task ClearAlertIfNeeded(
			NpgsqlConnection db,
			Notification notification,
			NotificationAction action
		) {
			switch (notification.EventType) {
				case NotificationEventType.Aotd:
				case NotificationEventType.Follower:
				case NotificationEventType.Loopback:
				case NotificationEventType.Post:
				case NotificationEventType.Reply:
					if (
						(
							action == NotificationAction.View ||
							action == NotificationAction.Reply
						) &&
						!notification.DateAlertCleared.HasValue
					) {
						await ClearAlert(
							db: db,
							userAccountId: notification.UserAccountId,
							receiptId: notification.ReceiptId
						);
					}
					break;
			}
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
							await CreateOpenInteraction(
								db: db,
								receiptId: token.ReceiptId
							);
							return (NotificationAction.Open, null);
						case NotificationAction.View:
							return (
								NotificationAction.View,
								await CreateViewInteraction(
									db: db,
									notification: await db.GetNotification(token.ReceiptId),
									channel: token.Channel.Value,
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