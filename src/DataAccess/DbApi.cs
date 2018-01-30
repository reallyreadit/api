using System.Data;
using Npgsql;
using Dapper;
using api.DataAccess.Models;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using api.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace api.DataAccess {
    public static class DbApi {
		// using this factory method since Npgsql seems to forget globally mapped types
		public static NpgsqlConnection CreateConnection(string connectionString) {
			var conn = new NpgsqlConnection(connectionString);
			conn.Open();
			conn.MapComposite<CreateArticleAuthor>();
			conn.MapComposite<CreateBulkMailingRecipient>();
			conn.MapEnum<SourceRuleAction>();
			conn.MapEnum<UserAccountRole>();
			return conn;
		}
		#region article_api
		public static Guid CreateArticle(
			this NpgsqlConnection conn,
			string title,
			string slug,
			Guid sourceId,
			DateTime? datePublished,
			DateTime? dateModified,
			string section,
			string description,
			CreateArticleAuthor[] authors,
			string[] tags
		) => conn.QuerySingleOrDefault<Guid>(
			sql: "article_api.create_article",
			param: new {
				title,
				slug,
				source_id = sourceId,
				date_published = datePublished,
				date_modified = dateModified,
				section,
				description,
				authors,
				tags
			},
			commandType: CommandType.StoredProcedure
		);
		public static Comment CreateComment(this NpgsqlConnection conn, string text, Guid articleId, Guid? parentCommentId, Guid userAccountId) => conn.QuerySingleOrDefault<Comment>(
			sql: "article_api.create_comment",
			param: new {
				text,
				article_id = articleId,
				parent_comment_id = parentCommentId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static Page CreatePage(this NpgsqlConnection conn, Guid articleId, int number, int wordCount, int readableWordCount, string url) => conn.QuerySingleOrDefault<Page>(
			sql: "article_api.create_page",
			param: new {
				article_id = articleId,
				number,
				word_count = wordCount,
				readable_word_count = readableWordCount,
				url
			},
			commandType: CommandType.StoredProcedure
		);
		public static Source CreateSource(this NpgsqlConnection conn, string name, string url, string hostname, string slug) => conn.QuerySingleOrDefault<Source>(
			sql: "article_api.create_source",
			param: new { name, url, hostname, slug },
			commandType: CommandType.StoredProcedure
		);
		public static UserPage CreateUserPage(this NpgsqlConnection conn, Guid pageId, Guid userAccountId) => conn.QuerySingleOrDefault<UserPage>(
			sql: "article_api.create_user_page",
			param: new {
				page_id = pageId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static void DeleteUserArticle(this NpgsqlConnection conn, Guid articleId, Guid userAccountId) => conn.Execute(
			sql: "article_api.delete_user_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static UserArticle FindArticle(this NpgsqlConnection conn, string slug) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.find_article",
			param: new { slug },
			commandType: CommandType.StoredProcedure
		);
		public static Page FindPage(this NpgsqlConnection conn, string url) => conn.QuerySingleOrDefault<Page>(
			sql: "article_api.find_page",
			param: new { url },
			commandType: CommandType.StoredProcedure
		);
		public static Source FindSource(this NpgsqlConnection conn, string sourceHostname) => conn.QuerySingleOrDefault<Source>(
			sql: "article_api.find_source",
			param: new { source_hostname = sourceHostname },
			commandType: CommandType.StoredProcedure
		);
		public static UserArticle FindUserArticle(this NpgsqlConnection conn, string slug, Guid userAccountId) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.find_user_article",
			param: new { slug, user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserArticle> GetAotd(this NpgsqlConnection conn) => await conn.QuerySingleOrDefaultAsync<UserArticle>(
			sql: "article_api.get_aotd",
			commandType: CommandType.StoredProcedure
		);
		public static Comment GetComment(this NpgsqlConnection conn, Guid commentId) => conn.QuerySingleOrDefault<Comment>(
			sql: "article_api.get_comment",
			param: new { comment_id = commentId },
			commandType: CommandType.StoredProcedure
		);
		public static Page GetPage(this NpgsqlConnection conn, Guid pageId) => conn.QuerySingleOrDefault<Page>(
			sql: "article_api.get_page",
			param: new { page_id = pageId },
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<SourceRule> GetSourceRules(this NpgsqlConnection conn) => conn.Query<SourceRule>(
			sql: "article_api.get_source_rules",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserArticle> GetUserAotd(this NpgsqlConnection conn, Guid userAccountId) => await conn.QuerySingleOrDefaultAsync<UserArticle>(
			sql: "article_api.get_user_aotd",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static UserArticle GetUserArticle(this NpgsqlConnection conn, Guid articleId, Guid userAccountId) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.get_user_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static UserPage GetUserPage(this NpgsqlConnection conn, Guid pageId, Guid userAccountId) => conn.QuerySingleOrDefault<UserPage>(
			sql: "article_api.get_user_page",
			param: new {
				page_id = pageId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<Comment> ListComments(this NpgsqlConnection conn, Guid articleId) => conn.Query<Comment>(
			sql: "article_api.list_comments",
			param: new { article_id = articleId },
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<UserArticle>> ListHotTopics(this NpgsqlConnection conn, int pageNumber, int pageSize) => PageResult<UserArticle>.Create(
			items: await conn.QueryAsync<UserArticlePageResult>(
				sql: "article_api.list_hot_topics",
				param: new
				{
					page_number = pageNumber,
					page_size = pageSize
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static PageResult<Comment> ListReplies(this NpgsqlConnection conn, Guid userAccountId, int pageNumber, int pageSize) => PageResult<Comment>.Create(
			items: conn.Query<CommentPageResult>(
				sql: "article_api.list_replies",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static PageResult<UserArticle> ListStarredArticles(this NpgsqlConnection conn, Guid userAccountId, int pageNumber, int pageSize) => PageResult<UserArticle>.Create(
			items: conn.Query<UserArticlePageResult>(
				sql: "article_api.list_starred_articles",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static PageResult<UserArticle> ListUserArticleHistory(this NpgsqlConnection conn, Guid userAccountId, int pageNumber, int pageSize) => PageResult<UserArticle>.Create(
			items: conn.Query<UserArticlePageResult>(
				sql: "article_api.list_user_article_history",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<UserArticle>> ListUserHotTopics(this NpgsqlConnection conn, Guid userAccountId, int pageNumber, int pageSize) => PageResult<UserArticle>.Create(
			items: await conn.QueryAsync<UserArticlePageResult>(
				sql: "article_api.list_user_hot_topics",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static void ReadComment(this NpgsqlConnection conn, Guid commentId) => conn.Execute(
			sql: "article_api.read_comment",
			param: new { comment_id = commentId },
			commandType: CommandType.StoredProcedure
		);
		public static void StarArticle(this NpgsqlConnection conn, Guid userAccountId, Guid articleId) => conn.Execute(
			sql: "article_api.star_article",
			param: new {
				user_account_id = userAccountId,
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static void UnstarArticle(this NpgsqlConnection conn, Guid userAccountId, Guid articleId) => conn.Execute(
			sql: "article_api.unstar_article",
			param: new {
				user_account_id = userAccountId,
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static UserPage UpdateUserPage(this NpgsqlConnection conn, Guid userPageId, int[] readState) => conn.QuerySingleOrDefault<UserPage>(
			sql: "article_api.update_user_page",
			param: new {
				user_page_id = userPageId,
				read_state = readState
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region bulk_mailing_api
		public static Guid CreateBulkMailing(
			this NpgsqlConnection conn,
			string subject,
			string body,
			string list,
			Guid userAccountId,
			CreateBulkMailingRecipient[] recipients
		) => conn.QuerySingleOrDefault<Guid>(
			sql: "bulk_mailing_api.create_bulk_mailing",
			param: new {
				subject, body, list,
				user_account_id = userAccountId,
				recipients
			},
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<BulkMailing> ListBulkMailings(this NpgsqlConnection conn) => conn.Query<BulkMailing>(
			sql: "bulk_mailing_api.list_bulk_mailings",
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<EmailBounce> ListEmailBounces(this NpgsqlConnection conn) => conn.Query<EmailBounce>(
			sql: "bulk_mailing_api.list_email_bounces",
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region user_account_api
		public static void AckNewReply(this NpgsqlConnection conn, Guid userAccountId) => conn.Execute(
			sql: "user_account_api.ack_new_reply",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static void ChangeEmailAddress(this NpgsqlConnection conn, Guid userAccountId, string email) {
			try {
				conn.Execute(
					sql: "user_account_api.change_email_address",
					param: new { user_account_id = userAccountId, email },
					commandType: CommandType.StoredProcedure
				);
			} catch (NpgsqlException ex) when (ex.Data.Contains("ConstraintName")) {
				if (String.Equals(ex.Data["ConstraintName"], "user_account_email_key")) {
					throw new ValidationException("DuplicateEmail");
				}
				throw ex;
			}
		}
		public static void ChangePassword(this NpgsqlConnection conn, Guid userAccountId, byte[] passwordHash, byte[] passwordSalt) => conn.Execute(
			sql: "user_account_api.change_password",
			param: new {
				user_account_id = userAccountId,
				password_hash = passwordHash,
				password_salt = passwordSalt
			},
			commandType: CommandType.StoredProcedure
		);
		public static bool CompletePasswordResetRequest(this NpgsqlConnection conn, Guid passwordResetRequestId) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.complete_password_reset_request",
			param: new { password_reset_request_id = passwordResetRequestId },
			commandType: CommandType.StoredProcedure
		);
		public static bool ConfirmEmailAddress(this NpgsqlConnection conn, Guid emailConfirmationId) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.confirm_email_address",
			param: new { email_confirmation_id = emailConfirmationId },
			commandType: CommandType.StoredProcedure
		);
		public static EmailConfirmation CreateEmailConfirmation(this NpgsqlConnection conn, Guid userAccountId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.create_email_confirmation",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static PasswordResetRequest CreatePasswordResetRequest(this NpgsqlConnection conn, Guid userAccountId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.create_password_reset_request",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static UserAccount CreateUserAccount(this NpgsqlConnection conn, string name, string email, byte[] passwordHash, byte[] passwordSalt) {
			try {
				return conn.QuerySingleOrDefault<UserAccount>("user_account_api.create_user_account", new {
					name = name,
					email = email,
					password_hash = passwordHash,
					password_salt = passwordSalt
				}, commandType: CommandType.StoredProcedure);
			} catch (NpgsqlException ex) when (ex.Data.Contains("ConstraintName")) {
				if (String.Equals(ex.Data["ConstraintName"], "user_account_name_key")) {
					throw new ValidationException("DuplicateName");
				}
				if (String.Equals(ex.Data["ConstraintName"], "user_account_email_key")) {
					throw new ValidationException("DuplicateEmail");
				}
				throw ex;
			}
		}
		public static UserAccount FindUserAccount(this NpgsqlConnection conn, string email) => conn.QuerySingleOrDefault<UserAccount>(
			sql: "user_account_api.find_user_account",
			param: new { email },
			commandType: CommandType.StoredProcedure
		);
		public static EmailConfirmation GetEmailConfirmation(this NpgsqlConnection conn, Guid emailConfirmationId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.get_email_confirmation",
			param: new { email_confirmation_id = emailConfirmationId },
			commandType: CommandType.StoredProcedure
		);
		public static PasswordResetRequest GetLatestPasswordResetRequest(this NpgsqlConnection conn, Guid userAccountId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.get_latest_password_reset_request",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static EmailConfirmation GetLatestUnconfirmedEmailConfirmation(this NpgsqlConnection conn, Guid userAccountId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.get_latest_unconfirmed_email_confirmation",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static Comment GetLatestUnreadReply(this NpgsqlConnection conn, Guid userAccountId) => conn.QuerySingleOrDefault<Comment>(
			sql: "user_account_api.get_latest_unread_reply",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static PasswordResetRequest GetPasswordResetRequest(this NpgsqlConnection conn, Guid passwordResetRequestId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.get_password_reset_request",
			param: new { password_reset_request_id = passwordResetRequestId },
			commandType: CommandType.StoredProcedure
		);
		public static UserAccount GetUserAccount(this NpgsqlConnection conn, Guid userAccountId) => conn.QuerySingleOrDefault<UserAccount>(
			sql: "user_account_api.get_user_account",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static void RecordNewReplyDesktopNotification(this NpgsqlConnection conn, Guid userAccountId) => conn.Execute(
			sql: "user_account_api.record_new_reply_desktop_notification",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static bool IsEmailAddressConfirmed(this NpgsqlConnection conn, Guid userAccountId, string email) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.is_email_address_confirmed",
			param: new { user_account_id = userAccountId, email },
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<UserAccount> ListUserAccounts(this NpgsqlConnection conn) => conn.Query<UserAccount>(
			sql: "user_account_api.list_user_accounts",
			commandType: CommandType.StoredProcedure
		);
		public static UserAccount UpdateContactPreferences(
			this NpgsqlConnection conn,
			Guid userAccountId,
			bool receiveWebsiteUpdates,
			bool receiveSuggestedReadings
		) => conn.QuerySingleOrDefault<UserAccount>(
			sql: "user_account_api.update_contact_preferences",
			param: new {
				user_account_id = userAccountId,
				receive_website_updates = receiveWebsiteUpdates,
				receive_suggested_readings = receiveSuggestedReadings
			},
			commandType: CommandType.StoredProcedure
		);
		public static UserAccount UpdateNotificationPreferences(
			this NpgsqlConnection conn,
			Guid userAccountId,
			bool receiveReplyEmailNotifications,
			bool receiveReplyDesktopNotifications
		) => conn.QuerySingleOrDefault<UserAccount>(
			sql: "user_account_api.update_notification_preferences",
			param: new {
				user_account_id = userAccountId,
				receive_reply_email_notifications = receiveReplyEmailNotifications,
				receive_reply_desktop_notifications = receiveReplyDesktopNotifications
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion
	}
}