using System;
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
			using (
				var db = new NpgsqlConnection(
					connectionString: databaseOptions.ConnectionString
				)
			) {
				var dispatch = await db.CreateFollowerNotification(
					followingId: following.Id,
					followerId: following.FollowerUserAccountId,
					followeeId: following.FolloweeuserAccountId
				);
				if (dispatch?.ViaEmail ?? false) {
					// send email
					Console.WriteLine("Send follower via email");
				}
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
				await apnsService.Send(
					new ApnsNotification(
						payload: new ApnsPayload(
							applePayload: new ApnsApplePayload(
								alert: dispatch.ViaPush ?
									new ApnsAlert(
										title: "Re: " + comment.ArticleTitle,
										subtitle: comment.UserAccount,
										body: comment.Text
									) :
									null,
								badge: dispatch.GetTotalBadgeCount()
							),
							alertStatus: dispatch
						),
						tokens: dispatch.PushDeviceTokens
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
						await db.ClearAlert(
							receiptId: notification.ReceiptId
						);
					}
					break;
			}
		}
		public async Task CreateOpenInteraction(
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
		public async Task<string> CreateViewInteraction(
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
			return url;
		}
	}
}