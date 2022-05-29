// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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
			UserAccount user,
			bool tweet,
			ClientAnalytics analytics
		) {
			Comment comment;
			using (var dbConnection = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				comment = await dbConnection.CreateComment(
					text: text,
					articleId: articleId,
					parentCommentId: null,
					userAccountId: user.Id,
					analytics: analytics
				);
			}
			await notificationService.CreateLoopbackNotifications(
				comment: comment
			);
			await notificationService.CreatePostNotifications(
				userAccountId: user.Id,
				userName: comment.UserAccount,
				articleId: comment.ArticleId,
				articleTitle: comment.ArticleTitle,
				commentId: comment.Id,
				commentText: comment.Text,
				silentPostId: null
			);
			if (tweet) {
				await twitterAuth.TweetPostComment(
					comment: comment,
					user: user
				);
			}
			return comment;
		}
		public async Task<Comment> PostReply(
			string text,
			long articleId,
			long parentCommentId,
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
			await notificationService.CreateReplyNotification(
				comment: comment
			);
			return comment;
		}
		public async Task<SilentPost> PostSilentPost(
			Article article,
			UserAccount user,
			bool tweet,
			ClientAnalytics analytics
		) {
			SilentPost post;
			using (var dbConnection = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				post = await dbConnection.CreateSilentPost(
					userAccountId: user.Id,
					articleId: article.Id,
					analytics: analytics
				);
			}
			await notificationService.CreatePostNotifications(
				userAccountId: user.Id,
				userName: user.Name,
				articleId: article.Id,
				articleTitle: article.Title,
				commentId: null,
				commentText: null,
				silentPostId: post.Id
			);
			if (tweet) {
				await twitterAuth.TweetSilentPost(
					silentPost: post,
					article: article,
					user: user
				);
			}
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