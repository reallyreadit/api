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
using NpgsqlTypes;
using System.Data.Common;
using api.Security;
using SesNotification = api.Messaging.AmazonSesNotifications.Notification;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using api.Analytics;

namespace api.DataAccess {
    public static class DbApi {
		private static string SerializeToJson(object value) => JsonConvert.SerializeObject(
			value: value,
			settings: new JsonSerializerSettings() {
				ContractResolver = new DefaultContractResolver() {
					NamingStrategy = new SnakeCaseNamingStrategy()
				}
			}
		);
		private static string SerializeToJson(RequestAnalytics analytics) {
			return SerializeToJson(
				new {
					Client = new {
						Type = ClientTypeDictionary.EnumToString[analytics.Client.Type],
						Version = analytics.Client.Version,
						Mode = analytics.Client.Mode
					},
					Context = analytics.Context
				}
			);
		}
		#region analytics
		public static Task<IEnumerable<KeyMetricsReportRow>> GetKeyMetrics(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => conn.QueryAsync<KeyMetricsReportRow>(
			sql: "analytics.get_key_metrics",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task LogExtensionInstallation(
			this NpgsqlConnection conn,
			Guid installationId,
			long? userAccountId,
			string platform
		) => conn.ExecuteAsync(
			sql: "analytics.log_extension_installation",
			param: new {
				installation_id = installationId,
				user_account_id = userAccountId,
				platform
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task LogExtensionRemoval(
			this NpgsqlConnection conn,
			Guid installationId,
			long? userAccountId
		) => conn.ExecuteAsync(
			sql: "analytics.log_extension_removal",
			param: new {
				installation_id = installationId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task LogExtensionRemovalFeedback(
			this NpgsqlConnection conn,
			Guid installationId,
			string reason
		) => conn.ExecuteAsync(
			sql: "analytics.log_extension_removal_feedback",
			param: new {
				installation_id = installationId,
				reason
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion
		#region article_api
		public static long CreateArticle(
			this NpgsqlConnection conn,
			string title,
			string slug,
			long sourceId,
			DateTime? datePublished,
			DateTime? dateModified,
			string section,
			string description,
			string[] authorNames,
			string[] authorUrls,
			string[] tags
		) => conn.QuerySingleOrDefault<long>(
			sql: "article_api.create_article",
			param: new {
				title,
				slug,
				source_id = sourceId,
				date_published = datePublished,
				date_modified = dateModified,
				section,
				description,
				author_names = authorNames,
				author_urls = authorUrls,
				tags
			},
			commandType: CommandType.StoredProcedure
		);
		public static Comment CreateComment(
			this NpgsqlConnection conn,
			string text,
			long articleId,
			long? parentCommentId,
			long userAccountId,
			RequestAnalytics analytics
		) => conn.QuerySingleOrDefault<Comment>(
			sql: "article_api.create_comment",
			param: new {
				text,
				article_id = articleId,
				parent_comment_id = parentCommentId,
				user_account_id = userAccountId,
				analytics = SerializeToJson(analytics)
			},
			commandType: CommandType.StoredProcedure
		);
		public static Page CreatePage(this NpgsqlConnection conn, long articleId, int number, int wordCount, int readableWordCount, string url) => conn.QuerySingleOrDefault<Page>(
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
		public static UserArticle CreateUserArticle(
			this NpgsqlConnection conn,
			long articleId,
			long userAccountId,
			int readableWordCount,
			RequestAnalytics analytics
		) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.create_user_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId,
				readable_word_count = readableWordCount,
				analytics = SerializeToJson(analytics)
			},
			commandType: CommandType.StoredProcedure
		);
		public static Article FindArticle(
			this NpgsqlConnection conn,
			string slug,
			long? userAccountId
		) => conn.QuerySingleOrDefault<Article>(
			sql: "article_api.find_article",
			param: new {
				slug,
				user_account_id = userAccountId
			},
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
		public static async Task<Article> GetArticle(
			this NpgsqlConnection conn,
			long articleId,
			long? userAccountId
		) => await conn.QuerySingleOrDefaultAsync<Article>(
			sql: "article_api.get_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<Article>> GetArticleHistory(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<Article>.Create(
			items: await conn.QueryAsync<ArticlePageResult>(
				sql: "article_api.get_article_history",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static Comment GetComment(this NpgsqlConnection conn, long commentId) => conn.QuerySingleOrDefault<Comment>(
			sql: "article_api.get_comment",
			param: new { comment_id = commentId },
			commandType: CommandType.StoredProcedure
		);
		public static Page GetPage(this NpgsqlConnection conn, long pageId) => conn.QuerySingleOrDefault<Page>(
			sql: "article_api.get_page",
			param: new { page_id = pageId },
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<SourceRule> GetSourceRules(this NpgsqlConnection conn) => conn.Query<SourceRule>(
			sql: "article_api.get_source_rules",
			commandType: CommandType.StoredProcedure
		);
		public static PageResult<Article> GetStarredArticles(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<Article>.Create(
			items: conn.Query<ArticlePageResult>(
				sql: "article_api.get_starred_articles",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static UserArticle GetUserArticle(this NpgsqlConnection conn, long userArticleId) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.get_user_article",
			param: new {
				user_article_id = userArticleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static UserArticle GetUserArticle(this NpgsqlConnection conn, long articleId, long userAccountId) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.get_user_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<Comment> ListComments(this NpgsqlConnection conn, long articleId) => conn.Query<Comment>(
			sql: "article_api.list_comments",
			param: new { article_id = articleId },
			commandType: CommandType.StoredProcedure
		);
		public static PageResult<Comment> ListReplies(this NpgsqlConnection conn, long userAccountId, int pageNumber, int pageSize) => PageResult<Comment>.Create(
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
		public static Task<Rating> RateArticle(
			this NpgsqlConnection conn,
			long articleId,
			long userAccountId,
			int score
		) => conn.QuerySingleOrDefaultAsync<Rating>(
			sql: "article_api.rate_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId,
				score
			},
			commandType: CommandType.StoredProcedure
		);
		public static void ReadComment(this NpgsqlConnection conn, long commentId) => conn.Execute(
			sql: "article_api.read_comment",
			param: new { comment_id = commentId },
			commandType: CommandType.StoredProcedure
		);
		public static void StarArticle(this NpgsqlConnection conn, long userAccountId, long articleId) => conn.Execute(
			sql: "article_api.star_article",
			param: new {
				user_account_id = userAccountId,
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static void UnstarArticle(this NpgsqlConnection conn, long userAccountId, long articleId) => conn.Execute(
			sql: "article_api.unstar_article",
			param: new {
				user_account_id = userAccountId,
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static Page UpdatePage(this NpgsqlConnection conn, long pageId, int wordCount, int readableWordCount) => conn.QuerySingleOrDefault<Page>(
			sql: "article_api.update_page",
			param: new {
				page_id = pageId,
				word_count = wordCount,
				readable_word_count = readableWordCount
			},
			commandType: CommandType.StoredProcedure
		);
		public static UserArticle UpdateReadProgress(
			this NpgsqlConnection conn,
			long userArticleId,
			int[] readState,
			RequestAnalytics analytics
		) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.update_read_progress",
			param: new {
				user_article_id = userArticleId,
				read_state = readState,
				analytics = SerializeToJson(analytics)
			},
			commandType: CommandType.StoredProcedure
		);
		public static UserArticle UpdateUserArticle(this NpgsqlConnection conn, long userArticleId, int readableWordCount, int[] readState) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.update_user_article",
			param: new {
				user_article_id = userArticleId,
				readable_word_count = readableWordCount,
				read_state = readState
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region bulk_mailing_api
		public static long CreateBulkMailing(
			this NpgsqlConnection conn,
			string subject,
			string body,
			string list,
			long userAccountId,
			long[] recipientIds,
			bool[] recipientResults
		) => conn.QuerySingleOrDefault<long>(
			sql: "bulk_mailing_api.create_bulk_mailing",
			param: new {
				subject, body, list,
				user_account_id = userAccountId,
				recipient_ids = recipientIds,
				recipient_results = recipientResults
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task CreateEmailNotification(
			this NpgsqlConnection conn,
			string notificationType,
			Messaging.AmazonSesNotifications.Mail mail,
			Messaging.AmazonSesNotifications.Bounce bounce,
			Messaging.AmazonSesNotifications.Complaint complaint
		) => conn.ExecuteAsync(
			sql: "bulk_mailing_api.create_email_notification",
			param: new {
				notification_type = notificationType,
				mail = SerializeToJson(mail),
				bounce = SerializeToJson(bounce),
				complaint = SerializeToJson(complaint)
			},
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<string> GetBlockedEmailAddresses(this NpgsqlConnection conn) => conn.Query<string>(
			sql: "bulk_mailing_api.get_blocked_email_addresses",
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<UserAccount> ListConfirmationReminderRecipients(this NpgsqlConnection conn) => conn.Query<UserAccount>(
			sql: "bulk_mailing_api.list_confirmation_reminder_recipients",
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<BulkMailing> ListBulkMailings(this NpgsqlConnection conn) => conn.Query<BulkMailing>(
			sql: "bulk_mailing_api.list_bulk_mailings",
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region community_reads
		public static async Task<Article> GetAotd(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QuerySingleOrDefaultAsync<Article>(
			sql: "community_reads.get_aotd",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<Article>> GetHighestRatedArticles(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			DateTime? sinceDate,
			int? minLength,
			int? maxLength
		) => PageResult<Article>.Create(
			items: await conn.QueryAsync<ArticlePageResult>(
				sql: "community_reads.get_highest_rated",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize,
					since_date = sinceDate,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<Article>> GetHotArticles(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<Article>.Create(
			items: await conn.QueryAsync<ArticlePageResult>(
				sql: "community_reads.get_hot",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<Article>> GetMostCommentedArticles(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			DateTime? sinceDate,
			int? minLength,
			int? maxLength
		) => PageResult<Article>.Create(
			items: await conn.QueryAsync<ArticlePageResult>(
				sql: "community_reads.get_most_commented",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize,
					since_date = sinceDate,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<Article>> GetMostReadArticles(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			DateTime? sinceDate,
			int? minLength,
			int? maxLength
		) => PageResult<Article>.Create(
			items: await conn.QueryAsync<ArticlePageResult>(
				sql: "community_reads.get_most_read",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize,
					since_date = sinceDate,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<Article>> GetTopArticles(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<Article>.Create(
			items: await conn.QueryAsync<ArticlePageResult>(
				sql: "community_reads.get_top",
				param: new {
					user_account_id = userAccountId,
					page_number = pageNumber,
					page_size = pageSize,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		#endregion

		#region core
		public static IEnumerable<TimeZone> GetTimeZones(
			this NpgsqlConnection conn
		) => conn.Query<TimeZone>(
			sql: "get_time_zones",
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region stats
		public static async Task<IEnumerable<LeaderboardRanking>> GetCurrentStreakLeaderboard(
			this NpgsqlConnection conn,
			long userAccountId,
			int maxRank
		) => await conn.QueryAsync<LeaderboardRanking>(
			sql: "stats.get_current_streak_leaderboard",
			param: new {
				user_account_id = userAccountId,
				max_rank = maxRank
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<ReadingTimeTotalsRow>> GetDailyReadingTimeTotals(
			this NpgsqlConnection conn,
			long userAccountId,
			int numberOfDays
		) => await conn.QueryAsync<ReadingTimeTotalsRow>(
			sql: "stats.get_daily_reading_time_totals",
			param: new {
				user_account_id = userAccountId,
				number_of_days = numberOfDays
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<LeaderboardRanking>> GetLongestReadLeaderboard(
			this NpgsqlConnection conn,
			int maxRank,
			DateTime? sinceDate
		) => await conn.QueryAsync<LeaderboardRanking>(
			sql: "stats.get_longest_read_leaderboard",
			param: new {
				max_rank = maxRank,
				since_date = sinceDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<ReadingTimeTotalsRow>> GetMonthlyReadingTimeTotals(
			this NpgsqlConnection conn,
			long userAccountId,
			int? numberOfMonths
		) => await conn.QueryAsync<ReadingTimeTotalsRow>(
			sql: "stats.get_monthly_reading_time_totals",
			param: new {
				user_account_id = userAccountId,
				number_of_months = numberOfMonths
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<LeaderboardRanking>> GetReadCountLeaderboard(
			this NpgsqlConnection conn,
			int maxRank,
			DateTime? sinceDate
		) => await conn.QueryAsync<LeaderboardRanking>(
			sql: "stats.get_read_count_leaderboard",
			param: new {
				max_rank = maxRank,
				since_date = sinceDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<LeaderboardRanking>> GetScoutLeaderboard(
			this NpgsqlConnection conn,
			int maxRank,
			DateTime? sinceDate
		) => await conn.QueryAsync<LeaderboardRanking>(
			sql: "stats.get_scout_leaderboard",
			param: new {
				max_rank = maxRank,
				since_date = sinceDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<LeaderboardRanking>> GetScribeLeaderboard(
			this NpgsqlConnection conn,
			int maxRank,
			DateTime? sinceDate
		) => await conn.QueryAsync<LeaderboardRanking>(
			sql: "stats.get_scribe_leaderboard",
			param: new {
				max_rank = maxRank,
				since_date = sinceDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<int> GetUserCount(
			this NpgsqlConnection conn
		) => await conn.QuerySingleOrDefaultAsync<int>(
			sql: "stats.get_user_count",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserLeaderboardRankings> GetUserLeaderboardRankings(
			this NpgsqlConnection conn,
			long userAccountId,
			DateTime? longestReadSinceDate,
			DateTime? scoutSinceDate,
			DateTime? scribeSinceDate
		) => await conn.QuerySingleOrDefaultAsync<UserLeaderboardRankings>(
			sql: "stats.get_user_leaderboard_rankings",
			param: new {
				user_account_id = userAccountId,
				longest_read_since_date = longestReadSinceDate,
				scout_since_date = scoutSinceDate,
				scribe_since_date = scribeSinceDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<int> GetUserReadCount(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QuerySingleOrDefaultAsync<int>(
			sql: "stats.get_user_read_count",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region user_account_api
		public static void AckNewReply(this NpgsqlConnection conn, long userAccountId) => conn.Execute(
			sql: "user_account_api.ack_new_reply",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static void ChangeEmailAddress(this NpgsqlConnection conn, long userAccountId, string email) {
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
		public static void ChangePassword(this NpgsqlConnection conn, long userAccountId, byte[] passwordHash, byte[] passwordSalt) => conn.Execute(
			sql: "user_account_api.change_password",
			param: new {
				user_account_id = userAccountId,
				password_hash = passwordHash,
				password_salt = passwordSalt
			},
			commandType: CommandType.StoredProcedure
		);
		public static bool CompletePasswordResetRequest(this NpgsqlConnection conn, long passwordResetRequestId) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.complete_password_reset_request",
			param: new { password_reset_request_id = passwordResetRequestId },
			commandType: CommandType.StoredProcedure
		);
		public static bool ConfirmEmailAddress(this NpgsqlConnection conn, long emailConfirmationId) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.confirm_email_address",
			param: new { email_confirmation_id = emailConfirmationId },
			commandType: CommandType.StoredProcedure
		);
		public static void CreateCaptchaResponse(this NpgsqlConnection conn, string actionVerified, CaptchaVerificationResponse response) => conn.Execute(
			sql: "user_account_api.create_captcha_response",
			param: new {
				action_verified = actionVerified,
				success = response.Success,
				score = response.Score,
				action = response.Action,
				challenge_ts = response.ChallengeTs,
				hostname = response.Hostname,
				error_codes = response.ErrorCodes
			},
			commandType: CommandType.StoredProcedure
		);
		public static EmailConfirmation CreateEmailConfirmation(this NpgsqlConnection conn, long userAccountId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.create_email_confirmation",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static PasswordResetRequest CreatePasswordResetRequest(this NpgsqlConnection conn, long userAccountId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.create_password_reset_request",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static UserAccount CreateUserAccount(
			this NpgsqlConnection conn,
			string name,
			string email,
			byte[] passwordHash,
			byte[] passwordSalt,
			long timeZoneId,
			RequestAnalytics analytics
		) {
			try {
				return conn.QuerySingleOrDefault<UserAccount>(
					sql: "user_account_api.create_user_account",
					param: new {
						name = name,
						email = email,
						password_hash = passwordHash,
						password_salt = passwordSalt,
						time_zone_id = timeZoneId,
						analytics = SerializeToJson(analytics)
					},
					commandType: CommandType.StoredProcedure
				);
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
		public static EmailConfirmation GetEmailConfirmation(this NpgsqlConnection conn, long emailConfirmationId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.get_email_confirmation",
			param: new { email_confirmation_id = emailConfirmationId },
			commandType: CommandType.StoredProcedure
		);
		public static PasswordResetRequest GetLatestPasswordResetRequest(this NpgsqlConnection conn, long userAccountId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.get_latest_password_reset_request",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static EmailConfirmation GetLatestUnconfirmedEmailConfirmation(this NpgsqlConnection conn, long userAccountId) => conn.QuerySingleOrDefault<EmailConfirmation>(
			sql: "user_account_api.get_latest_unconfirmed_email_confirmation",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static Comment GetLatestUnreadReply(this NpgsqlConnection conn, long userAccountId) => conn.QuerySingleOrDefault<Comment>(
			sql: "user_account_api.get_latest_unread_reply",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static PasswordResetRequest GetPasswordResetRequest(this NpgsqlConnection conn, long passwordResetRequestId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.get_password_reset_request",
			param: new { password_reset_request_id = passwordResetRequestId },
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserAccount> GetUserAccount(this NpgsqlConnection conn, long userAccountId) => await conn.QuerySingleOrDefaultAsync<UserAccount>(
			sql: "user_account_api.get_user_account",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static void RecordNewReplyDesktopNotification(this NpgsqlConnection conn, long userAccountId) => conn.Execute(
			sql: "user_account_api.record_new_reply_desktop_notification",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static bool IsEmailAddressConfirmed(this NpgsqlConnection conn, long userAccountId, string email) => conn.QuerySingleOrDefault<bool>(
			sql: "user_account_api.is_email_address_confirmed",
			param: new { user_account_id = userAccountId, email },
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<EmailConfirmation> ListEmailConfirmations(this NpgsqlConnection conn, long userAccountId) => conn.Query<EmailConfirmation>(
			sql: "user_account_api.list_email_confirmations",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<UserAccount> ListUserAccounts(this NpgsqlConnection conn) => conn.Query<UserAccount>(
			sql: "user_account_api.list_user_accounts",
			commandType: CommandType.StoredProcedure
		);
		public static UserAccount UpdateContactPreferences(
			this NpgsqlConnection conn,
			long userAccountId,
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
			long userAccountId,
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
		public static UserAccount UpdateTimeZone(
			this NpgsqlConnection conn,
			long userAccountId,
			long timeZoneId
		) => conn.QuerySingleOrDefault<UserAccount>(
			sql: "user_account_api.update_time_zone",
			param: new {
				user_account_id = userAccountId,
				time_zone_id = timeZoneId
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion
	}
}