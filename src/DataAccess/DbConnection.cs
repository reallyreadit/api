using System.Data;
using Npgsql;
using Dapper;
using api.DataAccess.Models;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using api.Configuration;
using System.Linq;

namespace api.DataAccess {
    public class DbConnection : IDisposable {
		private NpgsqlConnection conn;
		public DbConnection(IOptions<DatabaseOptions> dbOpts) {
			conn = new NpgsqlConnection(dbOpts.Value.ConnectionString);
			conn.Open();
			conn.MapComposite<CreateArticleAuthor>();
			conn.MapComposite<CreateBulkMailingRecipient>();
			conn.MapEnum<SourceRuleAction>();
			conn.MapEnum<UserAccountRole>();
		}
		public void Dispose() {
			conn.Dispose();
		}
		
		// article_api
		public Guid CreateArticle(
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
		public Comment CreateComment(string text, Guid articleId, Guid? parentCommentId, Guid userAccountId) => conn.QuerySingleOrDefault<Comment>(
			sql: "article_api.create_comment",
			param: new {
				text,
				article_id = articleId,
				parent_comment_id = parentCommentId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public Page CreatePage(Guid articleId, int number, int wordCount, int readableWordCount, string url) => conn.QuerySingleOrDefault<Page>(
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
		public Source CreateSource(string name, string url, string hostname, string slug) => conn.QuerySingleOrDefault<Source>(
			sql: "article_api.create_source",
			param: new { name, url, hostname, slug },
			commandType: CommandType.StoredProcedure
		);
		public UserPage CreateUserPage(Guid pageId, Guid userAccountId) => conn.QuerySingleOrDefault<UserPage>(
			sql: "article_api.create_user_page",
			param: new {
				page_id = pageId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public void DeleteUserArticle(Guid articleId, Guid userAccountId) => conn.Execute(
			sql: "article_api.delete_user_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public UserArticle FindArticle(string slug) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.find_article",
			param: new { slug },
			commandType: CommandType.StoredProcedure
		);
		public Page FindPage(string url) => conn.QuerySingleOrDefault<Page>(
			sql: "article_api.find_page",
			param: new { url },
			commandType: CommandType.StoredProcedure
		);
		public Source FindSource(string sourceHostname) => conn.QuerySingleOrDefault<Source>(
			sql: "article_api.find_source",
			param: new { source_hostname = sourceHostname },
			commandType: CommandType.StoredProcedure
		);
		public UserArticle FindUserArticle(string slug, Guid userAccountId) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.find_user_article",
			param: new { slug, user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public Comment GetComment(Guid commentId) => conn.QuerySingleOrDefault<Comment>(
			sql: "article_api.get_comment",
			param: new { comment_id = commentId },
			commandType: CommandType.StoredProcedure
		);
		public Page GetPage(Guid pageId) => conn.QuerySingleOrDefault<Page>(
			sql: "article_api.get_page",
			param: new { page_id = pageId },
			commandType: CommandType.StoredProcedure
		);
		public IEnumerable<SourceRule> GetSourceRules() => conn.Query<SourceRule>(
			sql: "article_api.get_source_rules",
			commandType: CommandType.StoredProcedure
		);
		public UserArticle GetUserArticle(Guid articleId, Guid userAccountId) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.get_user_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public UserPage GetUserPage(Guid pageId, Guid userAccountId) => conn.QuerySingleOrDefault<UserPage>(
			sql: "article_api.get_user_page",
			param: new {
				page_id = pageId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public IEnumerable<Comment> ListComments(Guid articleId) => conn.Query<Comment>(
			sql: "article_api.list_comments",
			param: new { article_id = articleId },
			commandType: CommandType.StoredProcedure
		);
		public PageResult<UserArticle> ListHotTopics(int pageNumber, int pageSize) => PageResult<UserArticle>.Create(
			items: conn.Query<UserArticlePageResult>(
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
		public PageResult<Comment> ListReplies(Guid userAccountId, int pageNumber, int pageSize) => PageResult<Comment>.Create(
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
		public PageResult<UserArticle> ListStarredArticles(Guid userAccountId, int pageNumber, int pageSize) => PageResult<UserArticle>.Create(
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
		public PageResult<UserArticle> ListUserArticleHistory(Guid userAccountId, int pageNumber, int pageSize) => PageResult<UserArticle>.Create(
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
		public PageResult<UserArticle> ListUserHotTopics(Guid userAccountId, int pageNumber, int pageSize) => PageResult<UserArticle>.Create(
			items: conn.Query<UserArticlePageResult>(
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
		public void ReadComment(Guid commentId) => conn.Execute(
			sql: "article_api.read_comment",
			param: new { comment_id = commentId },
			commandType: CommandType.StoredProcedure
		);
		public void StarArticle(Guid userAccountId, Guid articleId) => conn.Execute(
			sql: "article_api.star_article",
			param: new {
				user_account_id = userAccountId,
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public void UnstarArticle(Guid userAccountId, Guid articleId) => conn.Execute(
			sql: "article_api.unstar_article",
			param: new {
				user_account_id = userAccountId,
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public UserPage UpdateUserPage(Guid userPageId, int[] readState) => conn.QuerySingleOrDefault<UserPage>(
			sql: "article_api.update_user_page",
			param: new {
				user_page_id = userPageId,
				read_state = readState
			},
			commandType: CommandType.StoredProcedure
		);

		// bulk_mailing_api
		public Guid CreateBulkMailing(
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
		public IEnumerable<BulkMailing> ListBulkMailings() => conn.Query<BulkMailing>(
			sql: "bulk_mailing_api.list_bulk_mailings",
			commandType: CommandType.StoredProcedure
		);

		// user_account_api
		public void AckNewReply(Guid userAccountId) => conn.Execute(
			sql: "user_account_api.ack_new_reply",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public void ChangeEmailAddress(Guid userAccountId, string email) {
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
		public void ChangePassword(Guid userAccountId, byte[] passwordHash, byte[] passwordSalt) => conn.Execute(
			sql: "user_account_api.change_password",
			param: new {
				user_account_id = userAccountId,
				password_hash = passwordHash,
				password_salt = passwordSalt
			},
			commandType: CommandType.StoredProcedure
		);
		public bool CompletePasswordResetRequest(Guid passwordResetRequestId) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.complete_password_reset_request",
			param: new { password_reset_request_id = passwordResetRequestId },
			commandType: CommandType.StoredProcedure
		);
		public bool ConfirmEmailAddress(Guid emailConfirmationId) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.confirm_email_address",
			param: new { email_confirmation_id = emailConfirmationId },
			commandType: CommandType.StoredProcedure
		);
		public EmailConfirmation CreateEmailConfirmation(Guid userAccountId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.create_email_confirmation",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public PasswordResetRequest CreatePasswordResetRequest(Guid userAccountId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.create_password_reset_request",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public UserAccount CreateUserAccount(string name, string email, byte[] passwordHash, byte[] passwordSalt) {
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
		public UserAccount FindUserAccount(string email) => conn.QuerySingleOrDefault<UserAccount>(
			sql: "user_account_api.find_user_account",
			param: new { email },
			commandType: CommandType.StoredProcedure
		);
		public EmailConfirmation GetEmailConfirmation(Guid emailConfirmationId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.get_email_confirmation",
			param: new { email_confirmation_id = emailConfirmationId },
			commandType: CommandType.StoredProcedure
		);
		public PasswordResetRequest GetLatestPasswordResetRequest(Guid userAccountId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.get_latest_password_reset_request",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public EmailConfirmation GetLatestUnconfirmedEmailConfirmation(Guid userAccountId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.get_latest_unconfirmed_email_confirmation",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public Comment GetLatestUnreadReply(Guid userAccountId) => conn.QuerySingleOrDefault<Comment>(
			sql: "user_account_api.get_latest_unread_reply",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public PasswordResetRequest GetPasswordResetRequest(Guid passwordResetRequestId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.get_password_reset_request",
			param: new { password_reset_request_id = passwordResetRequestId },
			commandType: CommandType.StoredProcedure
		);
		public UserAccount GetUserAccount(Guid userAccountId) => conn.QuerySingleOrDefault<UserAccount>(
			sql: "user_account_api.get_user_account",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public void RecordNewReplyDesktopNotification(Guid userAccountId) => conn.Execute(
			sql: "user_account_api.record_new_reply_desktop_notification",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public bool IsEmailAddressConfirmed(Guid userAccountId, string email) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.is_email_address_confirmed",
			param: new { user_account_id = userAccountId, email },
			commandType: CommandType.StoredProcedure
		);
		public IEnumerable<UserAccount> ListUserAccounts() => conn.Query<UserAccount>(
			sql: "user_account_api.list_user_accounts",
			commandType: CommandType.StoredProcedure
		);
		public UserAccount UpdateContactPreferences(
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
		public UserAccount UpdateNotificationPreferences(
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
   }
}