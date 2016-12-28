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
        public void Dispose() {
            conn.Dispose();
        }
    }
}