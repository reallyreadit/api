using System;
using System.Threading.Tasks;
using api.Analytics;
using api.Authentication;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Markdown;
using api.Notifications;
using Markdig;
using Microsoft.Extensions.Options;
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
		private readonly TwitterAuthService twitterAuth;
		private readonly DatabaseOptions databaseOptions;
		public CommentingService(
			NotificationService notificationService,
			TwitterAuthService twitterAuth,
			IOptions<DatabaseOptions> databaseOptions 
		) {
			this.notificationService = notificationService;
			this.twitterAuth = twitterAuth;
			this.databaseOptions = databaseOptions.Value;
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
			long commentId,
			string text
		) {
			using (var dbConnection = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				return await dbConnection.CreateCommentAddendum(commentId, text);
			}
		}
		public async Task<Comment> DeleteComment(
			long commentId
		) {
			using (var dbConnection = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				return await dbConnection.DeleteComment(commentId);
			}
		}
		public async Task<Comment> PostComment(
			string text,
			long articleId,
			long? parentCommentId,
			long userAccountId,
			ClientAnalytics analytics
		) {
			Comment comment;
			using (var dbConnection = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				comment = await dbConnection.CreateComment(
					text: text,
					articleId: articleId,
					parentCommentId: parentCommentId,
					userAccountId: userAccountId,
					analytics: analytics
				);
			}
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
				await twitterAuth.TweetPostComment(comment);
			}
			return comment;
		}
		public async Task<SilentPost> PostSilentPost(
			long articleId,
			string articleSlug,
			long userAccountId,
			ClientAnalytics analytics
		) {
			SilentPost post;
			using (var dbConnection = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				post = await dbConnection.CreateSilentPost(
					userAccountId: userAccountId,
					articleId: articleId,
					analytics: analytics
				);
			}
			await twitterAuth.TweetSilentPost(
				silentPost: post,
				articleSlug: articleSlug
			);
			return post;
		}
		public async Task<Comment> ReviseComment(
			long commentId,
			string text
		) {
			using (var dbConnection = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				return await dbConnection.ReviseComment(commentId, text);
			}
		}
	}
}