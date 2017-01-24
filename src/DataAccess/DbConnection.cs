using System.Data;
using Npgsql;
using Dapper;
using api.DataAccess.Models;
using System;
using System.Collections.Generic;

namespace api.DataAccess {
    public class DbConnection : IDisposable {
		private NpgsqlConnection conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=postgres;Database=rrit");
		public DbConnection() {
			conn.Open();
		}
		public IEnumerable<Article> ListArticles() {
			return conn.Query<Article>("list_articles", commandType: CommandType.StoredProcedure);
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
		public UserAccount GetUserAccount(Guid userAccountId) {
			return conn.QuerySingleOrDefault<UserAccount>("get_user_account", new { user_account_id = userAccountId }, commandType: CommandType.StoredProcedure);
		}
		public UserAccount FindUserAccount(string userAccountName) {
			return conn.QuerySingleOrDefault<UserAccount>("find_user_account", new { user_account_name = userAccountName }, commandType: CommandType.StoredProcedure);
		}
		public Session CreateSession(Guid userAccountId) {
			return conn.QuerySingleOrDefault<Session>("create_session", new { user_account_id = userAccountId }, commandType: CommandType.StoredProcedure);
		}
		public Session GetSession(byte[] sessionKey) {
			return conn.QuerySingleOrDefault<Session>("get_session", new { session_key = sessionKey }, commandType: CommandType.StoredProcedure);
		}
		public void EndSession(byte[] sessionKey) {
			conn.Execute("end_session", new { session_key = sessionKey }, commandType: CommandType.StoredProcedure);
		}
		public Source FindSource(string sourceHostname) {
			return conn.QuerySingleOrDefault<Source>("find_source", new { source_hostname = sourceHostname }, commandType: CommandType.StoredProcedure);
		}
		public UserPage FindUserPage(string articleSlug, int pageNumber, Guid userAccountId) {
			return conn.QuerySingleOrDefault<UserPage>(
				sql: "find_user_page",
				param: new {
					article_slug = articleSlug,
					page_number = pageNumber,
					user_account_id = userAccountId
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public UserPage CreateUserPage(Guid pageId, Guid userAccountId, int[] readState, int percentComplete) {
			return conn.QuerySingleOrDefault(
				sql: "create_user_page",
				param: new {
					page_id = pageId,
					user_account_id = userAccountId,
					read_state = readState,
					percent_complete = percentComplete
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public UserPage UpdateUserPage(Guid userPageId, int[] readState, int percentComplete) {
			return conn.QuerySingleOrDefault(
				sql: "update_user_page",
				param: new {
					user_page_id = userPageId,
					read_state = readState,
					percent_complete = percentComplete
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public Article FindArticle(string slug) {
			return conn.QuerySingleOrDefault("find_article", new { slug }, commandType: CommandType.StoredProcedure);
		}
		public Article CreateArticle(string title, string slug, string author, DateTime? datePublished, Guid sourceId) {
			return conn.QuerySingleOrDefault(
				sql: "create_article",
				param: new {
					title,
					slug,
					author,
					date_published = datePublished,
					source_id = sourceId
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public Page CreatePage(Guid articleId, int number, int wordCount, string url) {
			return conn.QuerySingleOrDefault(
				sql: "create_page",
				param: new {
					article_id = articleId,
					number,
					word_count = wordCount,
					url
				},
				commandType: CommandType.StoredProcedure
			);
		}
		public Page GetPage(Guid articleId, int number) {
			return conn.QuerySingleOrDefault("get_page", new { article_id = articleId, number }, commandType: CommandType.StoredProcedure);
		}
        public void Dispose() {
            conn.Dispose();
        }
    }
}