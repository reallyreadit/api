using System.Data;
using Npgsql;
using Dapper;
using api.DataAccess.Models;
using System;
using System.Collections.Generic;

namespace api.DataAccess {
    public class DbConnection : IDisposable {
		private NpgsqlConnection conn = new NpgsqlConnection(Startup.DbConnectionString);
		public DbConnection() {
			conn.Open();
		}
		public Article CreateArticle(
			string title,
			string slug,
			Guid sourceId,
			DateTime? datePublished,
			DateTime? dateModified,
			string section,
			string description,
			CreateArticleAuthor[] authors,
			string[] tags
		) {
			return conn.QuerySingleOrDefault<Article>(
				sql: "create_article",
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
		}
		public Comment CreateComment(string text, Guid articleId, Guid? parentCommentId, Guid userAccountId) {
			return conn.QuerySingleOrDefault<Comment>(
				sql: "create_comment",
				param: new {
					text,
					article_id = articleId,
					parent_comment_id = parentCommentId,
					user_account_id = userAccountId
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public Page CreatePage(Guid articleId, int number, int wordCount, int readableWordCount, string url) {
			return conn.QuerySingleOrDefault<Page>(
				sql: "create_page",
				param: new {
					article_id = articleId,
					number,
					word_count = wordCount,
					readable_word_count = readableWordCount,
					url
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public Source CreateSource(string name, string url, string hostname, string slug) {
			return conn.QuerySingleOrDefault<Source>("create_source", new { name, url, hostname, slug }, commandType: CommandType.StoredProcedure);
		}
		public UserAccount CreateUserAccount(string name, string email, byte[] passwordHash, byte[] passwordSalt) {
			try {
				return conn.QuerySingleOrDefault<UserAccount>("create_user_account", new {
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
		public UserPage CreateUserPage(Guid pageId, Guid userAccountId) {
			return conn.QuerySingleOrDefault<UserPage>(
				sql: "create_user_page",
				param: new {
					page_id = pageId,
					user_account_id = userAccountId
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public void DeleteUserArticle(Guid articleId, Guid userAccountId) {
			conn.Execute(
				sql: "delete_user_article",
				param: new {
					article_id = articleId,
					user_account_id = userAccountId
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public Page FindPage(string url) {
			return conn.QuerySingleOrDefault<Page>("find_page", new { url }, commandType: CommandType.StoredProcedure);
		}
		public Source FindSource(string sourceHostname) {
			return conn.QuerySingleOrDefault<Source>("find_source", new { source_hostname = sourceHostname }, commandType: CommandType.StoredProcedure);
		}
		public UserAccount FindUserAccount(string userAccountName) {
			return conn.QuerySingleOrDefault<UserAccount>("find_user_account", new { user_account_name = userAccountName }, commandType: CommandType.StoredProcedure);
		}
		public UserArticle FindUserArticle(string slug, Guid? userAccountId = null) {
			return conn.QuerySingleOrDefault<UserArticle>("find_user_article", new { slug, user_account_id = userAccountId }, commandType: CommandType.StoredProcedure);
		}
		public Page GetPage(Guid pageId) {
			return conn.QuerySingleOrDefault<Page>("get_page", new { page_id = pageId }, commandType: CommandType.StoredProcedure);
		}
		public UserAccount GetUserAccount(Guid userAccountId) {
			return conn.QuerySingleOrDefault<UserAccount>("get_user_account", new { user_account_id = userAccountId }, commandType: CommandType.StoredProcedure);
		}
		public UserArticle GetUserArticle(Guid articleId, Guid userAccountId) {
			return conn.QuerySingleOrDefault<UserArticle>(
				sql: "get_user_article",
				param: new {
					article_id = articleId,
					user_account_id = userAccountId
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public UserPage GetUserPage(Guid pageId, Guid userAccountId) {
			return conn.QuerySingleOrDefault<UserPage>(
				sql: "get_user_page",
				param: new {
					page_id = pageId,
					user_account_id = userAccountId
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public IEnumerable<Comment> ListComments(Guid articleId) {
			return conn.Query<Comment>("list_comments", new { article_id = articleId }, commandType: CommandType.StoredProcedure);
		}
		public IEnumerable<Comment> ListReplies(Guid userAccountId) {
			return conn.Query<Comment>("list_replies", new { user_account_id = userAccountId }, commandType: CommandType.StoredProcedure);
		}
		public IEnumerable<UserArticle> ListUserArticles(Guid? userAccountId = null, int minCommentCount = 0, int minPercentComplete = 0) {
			return conn.Query<UserArticle>(
				sql: "list_user_articles",
				param: new {
					user_account_id = userAccountId,
					min_comment_count = minCommentCount,
					min_percent_complete = minPercentComplete
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public UserPage UpdateUserPage(Guid userPageId, int[] readState) {
			return conn.QuerySingleOrDefault<UserPage>(
				sql: "update_user_page",
				param: new {
					user_page_id = userPageId,
					read_state = readState
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public void Dispose() {
			conn.Dispose();
		}
    }
}