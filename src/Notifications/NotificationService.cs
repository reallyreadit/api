using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Encryption;
using api.Messaging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Notifications {
	public class NotificationService {
		private readonly DatabaseOptions databaseOptions;
		private readonly TokenizationOptions tokenizationOptions;
		private readonly EmailService emailService;
		public NotificationService(
			IOptions<DatabaseOptions> databaseOptions,
			IOptions<TokenizationOptions> tokenizationOptions,
			EmailService emailService
		) {
			this.databaseOptions = databaseOptions.Value;
			this.tokenizationOptions = tokenizationOptions.Value;
			this.emailService = emailService;
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
					if (dispatch.EmailAddress != null) {
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
					followeeId: following.FolloweeuserAccountId
				);
				if (dispatch?.EmailAddress != null) {
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
					if (dispatch.EmailAddress != null) {
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
					if (dispatch.EmailAddress != null) {
						// send email
						Console.WriteLine("Send post via email");
					}
				}
			}
		}
		public async Task CreateReplyNotification(
			Comment comment
		) {
			using (
				var db = new NpgsqlConnection(
					connectionString: databaseOptions.ConnectionString
				)
			) {
				var dispatch = await db.CreateReplyNotification(
					replyId: comment.Id,
					replyAuthorId: comment.UserAccountId,
					parentId: comment.ParentCommentId.Value
				);
				if (dispatch?.EmailAddress != null) {
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
			}
		}
		public NotificationToken DecryptTokenString(
			string tokenString
		) => new NotificationToken(
			tokenString: tokenString,
			key: tokenizationOptions.EncryptionKey
		);
	}
}