using System;
using System.Threading.Tasks;
using api.Analytics;
using api.DataAccess;
using api.DataAccess.Models;
using api.Markdown;
using api.Notifications;
using Markdig;
using Npgsql;

namespace api.Commenting {
	public class CommentingService {
		private static MarkdownPipeline CommentTextMarkdownHtmlPipeline = new MarkdownPipelineBuilder()
			.DisableNonHttpLinks()
			.DisableHtml()
			.UseAutoLinks()
			.Build();
		public static string RenderCommentTextToHtml(string commentText) =>
			commentText != null ?
				Markdig.Markdown.ToHtml(commentText, CommentTextMarkdownHtmlPipeline) :
				null;
		public static string RenderCommentTextToPlainText(string commentText) =>
			commentText != null ?
				Markdig.Markdown.ToPlainText(commentText) :
				null;
		private readonly NotificationService notificationService;
		public CommentingService(
			NotificationService notificationService
		) {
			this.notificationService = notificationService;
		}
		public bool CanReviseComment(
			Comment comment
		) => (
			DateTime.UtcNow.Subtract(comment.DateCreated) < TimeSpan.FromMinutes(3.5)
		);
		public bool IsCommentTextValid(
			string text
		) => (
			!String.IsNullOrWhiteSpace(text)
		);
		public async Task<Comment> CreateAddendum(
			NpgsqlConnection dbConnection,
			long commentId,
			string text
		) {
			return await dbConnection.CreateCommentAddendum(commentId, text);
		}
		public async Task<Comment> DeleteComment(
			NpgsqlConnection dbConnection,
			long commentId
		) {
			return await dbConnection.DeleteComment(commentId);
		}
		public async Task<Comment> PostComment(
			NpgsqlConnection dbConnection,
			string text,
			long articleId,
			long? parentCommentId,
			long userAccountId,
			ClientAnalytics analytics
		) {
			var comment = await dbConnection.CreateComment(
				text: text,
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
		public async Task<Comment> ReviseComment(
			NpgsqlConnection dbConnection,
			long commentId,
			string text
		) {
			return await dbConnection.ReviseComment(commentId, text);
		}
	}
}