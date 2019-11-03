using System;
using System.Net;
using System.Threading.Tasks;
using api.Analytics;
using api.DataAccess;
using api.DataAccess.Models;
using api.Notifications;
using Npgsql;

namespace api.Commenting {
	public class CommentingService {
		private readonly NotificationService notificationService;
		public CommentingService(
			NotificationService notificationService
		) {
			this.notificationService = notificationService;
		}
		public bool IsCommentTextValid(
			string text
		) => (
			!String.IsNullOrWhiteSpace(text)
		);
		public async Task<Comment> PostComment(
			NpgsqlConnection dbConnection,
			string text,
			long articleId,
			long? parentCommentId,
			long userAccountId,
			RequestAnalytics analytics
		) {
			var comment = await dbConnection.CreateComment(
				text: WebUtility.HtmlEncode(text),
				articleId: articleId,
				parentCommentId: parentCommentId,
				userAccountId: userAccountId,
				analytics: analytics
			);
			if (parentCommentId != null) {
				await notificationService.CreateReplyNotification(
					comment: comment
				);
			} else {
				await notificationService.CreateLoopbackNotifications(
					comment: comment
				);
				await notificationService.CreatePostNotifications(
					userAccountId: userAccountId,
					userName: comment.UserAccount,
					articleId: comment.ArticleId,
					articleTitle: comment.ArticleTitle,
					commentId: comment.Id,
					commentText: comment.Text,
					silentPostId: null
				);
			}
			return comment;
		}
	}
}