using System.Data;
using Npgsql;
using Dapper;
using api.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using api.Security;
using api.Analytics;
using api.DataAccess.Serialization;
using api.DataAccess.Stats;
using System.Text.RegularExpressions;
using System.Linq;
using api.Authentication;

namespace api.DataAccess {
    public static class DbApi {
		#region analytics
		public static async Task<IEnumerable<ArticleIssuesReportRow>> GetArticleIssueReports(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => await conn.QueryAsync<ArticleIssuesReportRow>(
			sql: "analytics.get_article_issue_reports",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<long>> GetArticlesRequiringAuthorAssignmentsAsync(
			this NpgsqlConnection conn
		) => await conn.QueryAsync<long>(
			sql: "analytics.get_articles_requiring_author_assignments_v1",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<ConversionsReportRow>> GetConversions(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => await conn.QueryAsync<ConversionsReportRow>(
			sql: "analytics.get_conversions",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<DailyTotalsReportRow>> GetDailyTotals(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => await conn.QueryAsync<DailyTotalsReportRow>(
			sql: "analytics.get_daily_totals",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<MonthlyRecurringRevenueReportLineItem>> GetMonthlyRecurringRevenueReportAsync(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => await conn.QueryAsync<MonthlyRecurringRevenueReportLineItem>(
			sql: "analytics.get_monthly_recurring_revenue_report",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<RevenueReportLineItem>> GetRevenueReportAsync(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => await conn.QueryAsync<RevenueReportLineItem>(
			sql: "analytics.get_revenue_report",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<SignupsReportRow>> GetSignups(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => await conn.QueryAsync<SignupsReportRow>(
			sql: "analytics.get_signups",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<WeeklyUserActivityReport>> GetWeeklyUserActivityAsync(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => await conn.QueryAsync<WeeklyUserActivityReport>(
			sql: "analytics.get_weekly_user_activity",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task LogArticleIssueReport(
			this NpgsqlConnection conn,
			long articleId,
			long userAccountId,
			string issue,
			ClientAnalytics analytics
		) => conn.ExecuteAsync(
			sql: "analytics.log_article_issue_report",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId,
				issue,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task LogClientErrorReport(
			this NpgsqlConnection conn,
			string content,
			ClientAnalytics analytics
		) => conn.ExecuteAsync(
			sql: "analytics.log_client_error_report",
			param: new {
				content,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
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
		public static Task LogNewPlatformNotificationRequest(
			this NpgsqlConnection conn,
			string emailAddress,
			string ipAddress,
			string userAgent
		) => conn.ExecuteAsync(
			sql: "analytics.log_new_platform_notification_request",
			param: new {
				email_address = emailAddress,
				ip_address = ipAddress,
				user_agent = userAgent
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task LogOrientationAnalytics(
			this NpgsqlConnection conn,
			long userAccountId,
			int trackingPlayCount,
			bool trackingSkipped,
			int trackingDuration,
			int importPlayCount,
			bool importSkipped,
			int importDuration,
			NotificationAuthorizationRequestResult notificationsResult,
			bool notificationsSkipped,
			int notificationsDuration,
			Guid? shareResultId,
			bool shareSkipped,
			int shareDuration
		) => conn.ExecuteAsync(
			sql: "analytics.log_orientation_analytics",
			param: new {
				user_account_id = userAccountId,
				tracking_play_count = trackingPlayCount,
				tracking_skipped = trackingSkipped,
				tracking_duration = trackingDuration,
				import_play_count = importPlayCount,
				import_skipped = importSkipped,
				import_duration = importDuration,
				notifications_result = PostgresSerialization.SerializeEnum(notificationsResult),
				notifications_skipped = notificationsSkipped,
				notifications_duration = notificationsDuration,
				share_result_id = shareResultId,
				share_skipped = shareSkipped,
				share_duration = shareDuration
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task LogShareResult(
			this NpgsqlConnection conn,
			Guid id,
			ClientType clientType,
			long? userAccountId,
			string action,
			string activityType,
			bool? completed,
			string error
		) => conn.ExecuteAsync(
			sql: "analytics.log_share_result",
			param: new {
				id,
				client_type = PostgresSerialization.SerializeEnum(clientType),
				user_account_id = userAccountId,
				action,
				activity_type = activityType,
				completed,
				error
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<TwitterBotTweet> LogTwitterBotTweet(
			this NpgsqlConnection conn,
			string handle,
			long? articleId,
			long? commentId,
			string content,
			string tweetId
		) => await conn.QuerySingleOrDefaultAsync<TwitterBotTweet>(
			sql: "analytics.log_twitter_bot_tweet",
			param: new {
				handle,
				article_id = articleId,
				comment_id = commentId,
				content,
				tweet_id = tweetId
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion
		#region article_api
		public static async Task<ArticleAuthorAssignment> AssignAuthorToArticleAsync(
			this NpgsqlConnection conn,
			long articleId,
			long authorId,
			long assignedByUserAccountId
		) => await conn.QuerySingleOrDefaultAsync<ArticleAuthorAssignment>(
			sql: "article_api.assign_author_to_article",
			param: new {
				article_id = articleId,
				author_id = authorId,
				assigned_by_user_account_id = assignedByUserAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Source> AssignTwitterHandleToSource(
			this NpgsqlConnection conn,
			long sourceId,
			string twitterHandle,
			TwitterHandleAssignment twitterHandleAssignment
		) => await conn.QuerySingleOrDefaultAsync<Source>(
			sql: "article_api.assign_twitter_handle_to_source",
			param: new {
				source_id = sourceId,
				twitter_handle = twitterHandle,
				twitter_handle_assignment = PostgresSerialization.SerializeEnum(twitterHandleAssignment)
			},
			commandType: CommandType.StoredProcedure
		);
		public static long CreateArticle(
			this NpgsqlConnection conn,
			string title,
			string slug,
			long sourceId,
			DateTime? datePublished,
			DateTime? dateModified,
			string section,
			string description,
			AuthorMetadata[] authors,
			TagMetadata[] tags
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
				authors,
				tags
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
		public static async Task<ProvisionalUserArticle> CreateProvisionalUserArticle(
			this NpgsqlConnection conn,
			long articleId,
			long provisionalUserAccountId,
			int readableWordCount,
			bool markAsViewed,
			ClientAnalytics analytics
		) => await conn.QuerySingleOrDefaultAsync<ProvisionalUserArticle>(
			sql: "article_api.create_provisional_user_article",
			param: new {
				article_id = articleId,
				provisional_user_account_id = provisionalUserAccountId,
				readable_word_count = readableWordCount,
				mark_as_viewed = markAsViewed,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
			},
			commandType: CommandType.StoredProcedure
		);
		public static Source CreateSource(this NpgsqlConnection conn, string name, string url, string hostname, string slug) => conn.QuerySingleOrDefault<Source>(
			sql: "article_api.create_source",
			param: new { name, url, hostname, slug },
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserArticle> CreateUserArticle(
			this NpgsqlConnection conn,
			long articleId,
			long userAccountId,
			int readableWordCount,
			bool markAsViewed,
			ClientAnalytics analytics
		) => await conn.QuerySingleOrDefaultAsync<UserArticle>(
			sql: "article_api.create_user_article",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId,
				readable_word_count = readableWordCount,
				mark_as_viewed = markAsViewed,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
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
		public static async Task<Article> GetArticleById(
			this NpgsqlConnection conn,
			long articleId,
			long? userAccountId
		) => await conn.QuerySingleOrDefaultAsync<Article>(
			sql: "articles.get_article_by_id",
			param: new {
				article_id = articleId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Article> GetArticleBySlug(
			this NpgsqlConnection conn,
			string slug,
			long? userAccountId
		) => await conn.QuerySingleOrDefaultAsync<Article>(
			sql: "articles.get_article_by_slug",
			param: new {
				slug,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<Article>> GetArticlesAsync(
			this NpgsqlConnection conn,
			long[] articleIds,
			long? userAccountId = null
		) => await conn.QueryAsync<Article>(
			sql: "articles.get_articles",
			param: new {
				article_ids = articleIds,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Article> GetArticleForProvisionalUser(
			this NpgsqlConnection conn,
			long articleId,
			long provisionalUserAccountId
		) => await conn.QuerySingleOrDefaultAsync<Article>(
			sql: "articles.get_article_for_provisional_user",
			param: new {
				article_id = articleId,
				provisional_user_account_id = provisionalUserAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<long>> GetArticleHistory(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleAsync<ArticleIdsPage>(
				sql: "articles.get_article_history",
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
		public static async Task<ArticleImage> GetArticleImage(
			this NpgsqlConnection conn,
			long articleId
		) => await conn.QuerySingleOrDefaultAsync<ArticleImage>(
			sql: "article_api.get_article_image",
			param: new {
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<long>> GetArticlesByAuthorSlug(
			this NpgsqlConnection conn,
			string slug,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleOrDefaultAsync<ArticleIdsPage>(
				sql: "articles.get_articles_by_author_slug",
				param: new {
					slug,
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
		public static async Task<PageResult<long>> GetArticlesBySourceSlug(
			this NpgsqlConnection conn,
			string slug,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleOrDefaultAsync<ArticleIdsPage>(
				sql: "articles.get_articles_by_source_slug",
				param: new {
					slug,
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
		public static Page GetPage(this NpgsqlConnection conn, long pageId) => conn.QuerySingleOrDefault<Page>(
			sql: "article_api.get_page",
			param: new { page_id = pageId },
			commandType: CommandType.StoredProcedure
		);
		public static async Task<ProvisionalUserArticle> GetProvisionalUserArticle(
			this NpgsqlConnection conn,
			long articleId,
			long provisionalUserAccountId
		) => await conn.QuerySingleOrDefaultAsync<ProvisionalUserArticle>(
			sql: "article_api.get_provisional_user_article",
			param: new {
				article_id = articleId,
				provisional_user_account_id = provisionalUserAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Source> GetSourceOfArticle(
			this NpgsqlConnection conn,
			long articleId
		) => await conn.QuerySingleOrDefaultAsync<Source>(
			sql: "article_api.get_source_of_article",
			param: new {
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<SourceRule> GetSourceRules(this NpgsqlConnection conn) => conn.Query<SourceRule>(
			sql: "article_api.get_source_rules",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<long>> GetStarredArticles(
			this NpgsqlConnection conn,
			long userAccountId,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleOrDefaultAsync<ArticleIdsPage>(
				sql: "articles.get_starred_articles",
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
		public static async Task<UserArticle> MarkUserArticleAsViewedAsync(
			this NpgsqlConnection conn,
			long userArticleId
		) => await conn.QuerySingleOrDefaultAsync<UserArticle>(
			sql: "articles.mark_user_article_as_viewed",
			param: new {
				user_article_id = userArticleId
			},
			commandType: CommandType.StoredProcedure
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
		public static async Task<ArticleImage> SetArticleImage(
			this NpgsqlConnection conn,
			long articleId,
			long creatorUserId,
			string url
		) => await conn.QuerySingleOrDefaultAsync<ArticleImage>(
			sql: "article_api.set_article_image",
			param: new {
				article_id = articleId,
				creator_user_id = creatorUserId,
				url
			},
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
		public static async Task<ArticleAuthorAssignment> UnassignAuthorFromArticleAsync(
			this NpgsqlConnection conn,
			long articleId,
			long authorId,
			long unassignedByUserAccountId
		) => await conn.QuerySingleOrDefaultAsync<ArticleAuthorAssignment>(
			sql: "article_api.unassign_author_from_article",
			param: new {
				article_id = articleId,
				author_id = authorId,
				unassigned_by_user_account_id = unassignedByUserAccountId
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
		public static async Task<ProvisionalUserArticle> UpdateProvisionalReadProgress(
			this NpgsqlConnection conn,
			long provisionalUserAccountId,
			long articleId,
			int[] readState,
			ClientAnalytics analytics
		) => await conn.QuerySingleOrDefaultAsync<ProvisionalUserArticle>(
			sql: "article_api.update_provisional_read_progress",
			param: new {
				provisional_user_account_id = provisionalUserAccountId,
				article_id = articleId,
				read_state = readState,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
			},
			commandType: CommandType.StoredProcedure
		);
		public static UserArticle UpdateReadProgress(
			this NpgsqlConnection conn,
			long userArticleId,
			int[] readState,
			ClientAnalytics analytics
		) => conn.QuerySingleOrDefault<UserArticle>(
			sql: "article_api.update_read_progress",
			param: new {
				user_article_id = userArticleId,
				read_state = readState,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
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

		#region authors
		public static async Task<IEnumerable<long>> AssignContactStatusToAuthorsAsync(
			this NpgsqlConnection conn,
			AuthorContactStatusAssignment[] assignments
		) => await conn.QueryAsync<long>(
			sql: "authors.assign_contact_status_to_authors",
			param: new {
				assignments
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Author> AssignTwitterHandleToAuthor(
			this NpgsqlConnection conn,
			long authorId,
			string twitterHandle,
			TwitterHandleAssignment twitterHandleAssignment
		) => await conn.QuerySingleOrDefaultAsync<Author>(
			sql: "authors.assign_twitter_handle_to_author",
			param: new {
				author_id = authorId,
				twitter_handle = twitterHandle,
				twitter_handle_assignment = PostgresSerialization.SerializeEnum(twitterHandleAssignment)
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Author> CreateAuthorAsync(
			this NpgsqlConnection conn,
			string name,
			string slug
		) => await conn.QuerySingleOrDefaultAsync<Author>(
			sql: "authors.create_author",
			param: new {
				name,
				slug
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Author> GetAuthor(
			this NpgsqlConnection conn,
			string slug
		) => await conn.QuerySingleOrDefaultAsync<Author>(
			sql: "authors.get_author",
			param: new {
				slug
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Author> GetAuthorByUserAccountName(
			this NpgsqlConnection conn,
			string userAccountName
		) => await conn.QuerySingleOrDefaultAsync<Author>(
			sql: "authors.get_author_by_user_account_name",
			param: new {
				user_account_name = userAccountName
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<Author>> GetAuthorsOfArticle(
			this NpgsqlConnection conn,
			long articleId
		) => await conn.QueryAsync<Author>(
			sql: "authors.get_authors_of_article",
			param: new {
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserAccount> GetUserAccountByAuthorSlug(
			this NpgsqlConnection conn,
			string authorSlug
		) => await conn.QuerySingleOrDefaultAsync<UserAccount>(
			sql: "authors.get_user_account_by_author_slug",
			param: new {
				author_slug = authorSlug
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region notifications
		public static async Task<IEnumerable<NotificationReceipt>> ClearAllAlerts(
			this NpgsqlConnection conn,
			NotificationEventType type,
			long userAccountId
		) => await conn.QueryAsync<NotificationReceipt>(
			sql: "notifications.clear_all_alerts",
			param: new {
				type = PostgresSerialization.SerializeEnum(type),
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationReceipt> ClearAotdAlert(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QuerySingleOrDefaultAsync<NotificationReceipt>(
			sql: "notifications.clear_aotd_alert",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationReceipt> ClearAlert(
			this NpgsqlConnection conn,
			long receiptId
		) => await conn.QuerySingleOrDefaultAsync<NotificationReceipt>(
			sql: "notifications.clear_alert",
			param: new {
				receipt_id = receiptId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<NotificationEmailDispatch>> CreateAotdDigestNotifications(
			this NpgsqlConnection conn
		) => (
			await conn.QueryAsync<NotificationEmailDispatch>(
				sql: "notifications.create_aotd_digest_notifications",
				commandType: CommandType.StoredProcedure
			)
		);
		public static async Task<IEnumerable<NotificationAlertDispatch>> CreateAotdNotifications(
			this NpgsqlConnection conn,
			long articleId
		) => await conn.QueryAsync<NotificationAlertDispatch>(
			sql: "notifications.create_aotd_notifications",
			param: new {
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<NotificationEmailDispatch>> CreateCompanyUpdateNotifications(
			this NpgsqlConnection conn,
			long authorId,
			string subject,
			string body
		) => (
			await conn.QueryAsync<NotificationEmailDispatch>(
				sql: "notifications.create_company_update_notifications",
				param: new {
					author_id = authorId,
					subject,
					body
				},
				commandType: CommandType.StoredProcedure
			)
		);
		public static Task CreateEmailNotification(
			this NpgsqlConnection conn,
			string notificationType,
			Messaging.AmazonSesNotifications.Mail mail,
			Messaging.AmazonSesNotifications.Bounce bounce,
			Messaging.AmazonSesNotifications.Complaint complaint
		) => conn.ExecuteAsync(
			sql: "notifications.create_email_notification",
			param: new {
				notification_type = notificationType,
				mail = PostgresSerialization.SerializeJson(mail),
				bounce = PostgresSerialization.SerializeJson(bounce),
				complaint = PostgresSerialization.SerializeJson(complaint)
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<NotificationDigestDispatch<NotificationDigestFollower>>> CreateFollowerDigestNotifications(
			this NpgsqlConnection conn,
			NotificationEventFrequency frequency
		) => (
				await conn.QueryAsync<NotificationFollowerDigestDispatch>(
					sql: "notifications.create_follower_digest_notifications",
					param: new {
						frequency = PostgresSerialization.SerializeEnum(frequency)
					},
					commandType: CommandType.StoredProcedure
				)
			)
			.Aggregate(
				new List<NotificationDigestDispatch<NotificationDigestFollower>>(),
				(dispatches, row) => {
					var follower = new NotificationDigestFollower(
						followingId: row.FollowerFollowingId,
						dateFollowed: row.FollowerDateFollowed,
						userName: row.FollowerUserName
					);
					var dispatch = dispatches.SingleOrDefault(dispatch => dispatch.ReceiptId == row.ReceiptId);
					if (dispatch != null) {
						dispatch.Items.Add(follower);
					} else {
						dispatches.Add(
							new NotificationDigestDispatch<NotificationDigestFollower>(
								receiptId: row.ReceiptId,
								userAccountId: row.UserAccountId,
								userName: row.UserName,
								emailAddress: row.EmailAddress,
								items: new List<NotificationDigestFollower>() {
									follower
								}
							)
						);
					}
					return dispatches;
				}
			);
		public static Task<NotificationAlertDispatch> CreateFollowerNotification(
			this NpgsqlConnection conn,
			long followingId,
			long followerId,
			long followeeId
		) => conn.QuerySingleOrDefaultAsync<NotificationAlertDispatch>(
			sql: "notifications.create_follower_notification",
			param: new {
				following_id = followingId,
				follower_id = followerId,
				followee_id = followeeId
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task<NotificationInteraction> CreateNotificationInteraction(
			this NpgsqlConnection conn,
			long receiptId,
			NotificationChannel channel,
			NotificationAction action,
			string url = null,
			long? replyId = null
		) => conn.QuerySingleOrDefaultAsync<NotificationInteraction>(
			sql: "notifications.create_interaction",
			param: new {
				receipt_id = receiptId,
				channel = PostgresSerialization.SerializeEnum(channel),
				action = PostgresSerialization.SerializeEnum(action),
				url,
				reply_id = replyId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<NotificationDigestDispatch<NotificationDigestComment>>> CreateLoopbackDigestNotifications(
			this NpgsqlConnection conn,
			NotificationEventFrequency frequency
		) => (
				await conn.QueryAsync<NotificationCommentDigestDispatch>(
					sql: "notifications.create_loopback_digest_notifications",
					param: new {
						frequency = PostgresSerialization.SerializeEnum(frequency)
					},
					commandType: CommandType.StoredProcedure
				)
			)
			.Aggregate(
				new List<NotificationDigestDispatch<NotificationDigestComment>>(),
				(dispatches, row) => {
					var comment = new NotificationDigestComment(
						id: row.CommentId,
						dateCreated: row.CommentDateCreated,
						text: row.CommentText,
						addenda: row.CommentAddenda,
						author: row.CommentAuthor,
						articleId: row.CommentArticleId,
						articleTitle: row.CommentArticleTitle
					);
					var dispatch = dispatches.SingleOrDefault(dispatch => dispatch.ReceiptId == row.ReceiptId);
					if (dispatch != null) {
						dispatch.Items.Add(comment);
					} else {
						dispatches.Add(
							new NotificationDigestDispatch<NotificationDigestComment>(
								receiptId: row.ReceiptId,
								userAccountId: row.UserAccountId,
								userName: row.UserName,
								emailAddress: row.EmailAddress,
								items: new List<NotificationDigestComment>() {
									comment
								}
							)
						);
					}
					return dispatches;
				}
			);
		public static Task<IEnumerable<NotificationAlertDispatch>> CreateLoopbackNotifications(
			this NpgsqlConnection conn,
			long articleId,
			long commentId,
			long commentAuthorId
		) => conn.QueryAsync<NotificationAlertDispatch>(
			sql: "notifications.create_loopback_notifications",
			param: new {
				article_id = articleId,
				comment_id = commentId,
				comment_author_id = commentAuthorId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<NotificationDigestDispatch<NotificationDigestPost>>> CreatePostDigestNotifications(
			this NpgsqlConnection conn,
			NotificationEventFrequency frequency
		) => (
				await conn.QueryAsync<NotificationPostDigestDispatch>(
					sql: "notifications.create_post_digest_notifications",
					param: new {
						frequency = PostgresSerialization.SerializeEnum(frequency)
					},
					commandType: CommandType.StoredProcedure
				)
			)
			.Aggregate(
				new List<NotificationDigestDispatch<NotificationDigestPost>>(),
				(dispatches, row) => {
					var post = new NotificationDigestPost(
						commentId: row.PostCommentId,
						silentPostId: row.PostSilentPostId,
						dateCreated: row.PostDateCreated,
						commentText: row.PostCommentText,
						commentAddenda: row.PostCommentAddenda,
						author: row.PostAuthor,
						articleId: row.PostArticleId,
						articleTitle: row.PostArticleTitle
					);
					var dispatch = dispatches.SingleOrDefault(dispatch => dispatch.ReceiptId == row.ReceiptId);
					if (dispatch != null) {
						dispatch.Items.Add(post);
					} else {
						dispatches.Add(
							new NotificationDigestDispatch<NotificationDigestPost>(
								receiptId: row.ReceiptId,
								userAccountId: row.UserAccountId,
								userName: row.UserName,
								emailAddress: row.EmailAddress,
								items: new List<NotificationDigestPost>() {
									post
								}
							)
						);
					}
					return dispatches;
				}
			);
		public static Task<IEnumerable<NotificationPostAlertDispatch>> CreatePostNotifications(
			this NpgsqlConnection conn,
			long articleId,
			long posterId,
			long? commentId,
			long? silentPostId
		) => conn.QueryAsync<NotificationPostAlertDispatch>(
			sql: "notifications.create_post_notifications",
			param: new {
				article_id = articleId,
				poster_id = posterId,
				comment_id = commentId,
				silent_post_id = silentPostId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationPushAuthDenial> CreateNotificationPushAuthDenial(
			this NpgsqlConnection conn,
			long userAccountId,
			string installationId,
			string deviceName
		) => await conn.QuerySingleOrDefaultAsync<NotificationPushAuthDenial>(
			sql: "notifications.create_push_auth_denial",
			param: new {
				user_account_id = userAccountId,
				installation_id = installationId,
				device_name = deviceName
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<NotificationDigestDispatch<NotificationDigestComment>>> CreateReplyDigestNotifications(
			this NpgsqlConnection conn,
			NotificationEventFrequency frequency
		) => (
				await conn.QueryAsync<NotificationCommentDigestDispatch>(
					sql: "notifications.create_reply_digest_notifications",
					param: new {
						frequency = PostgresSerialization.SerializeEnum(frequency)
					},
					commandType: CommandType.StoredProcedure
				)
			)
			.Aggregate(
				new List<NotificationDigestDispatch<NotificationDigestComment>>(),
				(dispatches, row) => {
					var comment = new NotificationDigestComment(
						id: row.CommentId,
						dateCreated: row.CommentDateCreated,
						text: row.CommentText,
						addenda: row.CommentAddenda,
						author: row.CommentAuthor,
						articleId: row.CommentArticleId,
						articleTitle: row.CommentArticleTitle
					);
					var dispatch = dispatches.SingleOrDefault(dispatch => dispatch.ReceiptId == row.ReceiptId);
					if (dispatch != null) {
						dispatch.Items.Add(comment);
					} else {
						dispatches.Add(
							new NotificationDigestDispatch<NotificationDigestComment>(
								receiptId: row.ReceiptId,
								userAccountId: row.UserAccountId,
								userName: row.UserName,
								emailAddress: row.EmailAddress,
								items: new List<NotificationDigestComment>() {
									comment
								}
							)
						);
					}
					return dispatches;
				}
			);
		public static Task<NotificationAlertDispatch> CreateReplyNotification(
			this NpgsqlConnection conn,
			long replyId,
			long replyAuthorId,
			long parentId
		) => conn.QuerySingleOrDefaultAsync<NotificationAlertDispatch>(
			sql: "notifications.create_reply_notification",
			param: new {
				reply_id = replyId,
				reply_author_id = replyAuthorId,
				parent_id = parentId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationEmailDispatch> CreateTransactionalNotification(
			this NpgsqlConnection conn,
			long userAccountId,
			NotificationEventType eventType,
			long? emailConfirmationId,
			long? passwordResetRequestId
		) => (
			await conn.QuerySingleOrDefaultAsync<NotificationEmailDispatch>(
				sql: "notifications.create_transactional_notification",
				param: new {
					user_account_id = userAccountId,
					event_type = PostgresSerialization.SerializeEnum(eventType),
					email_confirmation_id = emailConfirmationId,
					password_reset_request_id = passwordResetRequestId
				},
				commandType: CommandType.StoredProcedure
			)
		);
		public static IEnumerable<string> GetBlockedEmailAddresses(this NpgsqlConnection conn) => conn.Query<string>(
			sql: "notifications.get_blocked_email_addresses",
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<BulkMailing> GetBulkMailings(this NpgsqlConnection conn) => conn.Query<BulkMailing>(
			sql: "notifications.get_bulk_mailings",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<Notification>> GetExtensionNotifications(
			this NpgsqlConnection conn,
			long userAccountId,
			DateTime sinceDate,
			long[] excludedReceiptIds
		) => await conn.QueryAsync<Notification>(
			sql: "notifications.get_extension_notifications",
			param: new {
				user_account_id = userAccountId,
				since_date = sinceDate,
				excluded_receipt_ids = excludedReceiptIds
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Notification> GetNotification(
			this NpgsqlConnection conn,
			long receiptId
		) => await conn.QuerySingleOrDefaultAsync<Notification>(
			sql: "notifications.get_notification",
			param: new {
				receipt_id = receiptId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<Notification>> GetNotifications(
			this NpgsqlConnection conn,
			params long[] receiptIds
		) => await conn.QueryAsync<Notification>(
			sql: "notifications.get_notifications",
			param: new {
				receipt_ids = receiptIds
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationPreference> GetNotificationPreference(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QuerySingleOrDefaultAsync<NotificationPreference>(
			sql: "notifications.get_preference",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<NotificationPushDevice>> GetRegisteredPushDevices(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QueryAsync<NotificationPushDevice>(
			sql: "notifications.get_registered_push_devices",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationPushDevice> RegisterNotificationPushDevice(
			this NpgsqlConnection conn,
			long userAccountId,
			string installationId,
			string name,
			string token
		) => await conn.QuerySingleOrDefaultAsync<NotificationPushDevice>(
			sql: "notifications.register_push_device",
			param: new {
				user_account_id = userAccountId,
				installation_id = installationId,
				name,
				token
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationPreference> SetNotificationPreference(
			this NpgsqlConnection conn,
			long userAccountId,
			NotificationPreferenceOptions options
		) => await conn.QuerySingleOrDefaultAsync<NotificationPreference>(
			sql: "notifications.set_preference",
			param: new {
				user_account_id = userAccountId,
				company_update_via_email = options.CompanyUpdateViaEmail,
				aotd_via_email = options.AotdViaEmail,
				aotd_via_extension = options.AotdViaExtension,
				aotd_via_push = options.AotdViaPush,
				aotd_digest_via_email = PostgresSerialization.SerializeEnum(options.AotdDigestViaEmail),
				reply_via_email = options.ReplyViaEmail,
				reply_via_extension = options.ReplyViaExtension,
				reply_via_push = options.ReplyViaPush,
				reply_digest_via_email = PostgresSerialization.SerializeEnum(options.ReplyDigestViaEmail),
				loopback_via_email = options.LoopbackViaEmail,
				loopback_via_extension = options.LoopbackViaExtension,
				loopback_via_push = options.LoopbackViaPush,
				loopback_digest_via_email = PostgresSerialization.SerializeEnum(options.LoopbackDigestViaEmail),
				post_via_email = options.PostViaEmail,
				post_via_extension = options.PostViaExtension,
				post_via_push = options.PostViaPush,
				post_digest_via_email = PostgresSerialization.SerializeEnum(options.PostDigestViaEmail),
				follower_via_email = options.FollowerViaEmail,
				follower_via_extension = options.FollowerViaExtension,
				follower_via_push = options.FollowerViaPush,
				follower_digest_via_email = PostgresSerialization.SerializeEnum(options.FollowerDigestViaEmail)
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationPushDevice> UnregisterNotificationPushDeviceByInstallationId(
			this NpgsqlConnection conn,
			string installationId,
			NotificationPushUnregistrationReason reason
		) => await conn.QuerySingleOrDefaultAsync<NotificationPushDevice>(
			sql: "notifications.unregister_push_device_by_installation_id",
			param: new {
				installation_id = installationId,
				reason = PostgresSerialization.SerializeEnum(reason)
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<NotificationPushDevice> UnregisterNotificationPushDeviceByToken(
			this NpgsqlConnection conn,
			string token,
			NotificationPushUnregistrationReason reason
		) => await conn.QuerySingleOrDefaultAsync<NotificationPushDevice>(
			sql: "notifications.unregister_push_device_by_token",
			param: new {
				token,
				reason = PostgresSerialization.SerializeEnum(reason)
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region community_reads
		public static async Task<PageResult<long>> GetAotdHistory(
			this NpgsqlConnection conn,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleOrDefaultAsync<ArticleIdsPage>(
				sql: "community_reads.get_aotd_history",
				param: new {
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
		public static async Task<IEnumerable<long>> GetAotds(
			this NpgsqlConnection conn,
			int dayCount
		) => await conn.QueryAsync<long>(
			sql: "community_reads.get_aotds",
			param: new {
				day_count = dayCount
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<long>> GetHotArticles(
			this NpgsqlConnection conn,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleOrDefaultAsync<ArticleIdsPage>(
				sql: "community_reads.get_hot",
				param: new {
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
		public static async Task<PageResult<long>> GetNewAotdContenders(
			this NpgsqlConnection conn,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleOrDefaultAsync<ArticleIdsPage>(
				sql: "community_reads.get_new_aotd_contenders",
				param: new {
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
		public static async Task<IEnumerable<SearchOption>> GetSearchOptions(
			this NpgsqlConnection conn
		) => await conn.QueryAsync<SearchOption>(
			sql: "community_reads.get_search_options",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<long>> GetTopArticles(
			this NpgsqlConnection conn,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleOrDefaultAsync<ArticleIdsPage>(
				sql: "community_reads.get_top",
				param: new {
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
		public static async Task<PageResult<long>> SearchArticles(
			this NpgsqlConnection conn,
			int pageNumber,
			int pageSize,
			string[] sourceSlugs,
			string[] authorSlugs,
			string[] tagSlugs,
			int? minLength,
			int? maxLength
		) => PageResult<long>.Create(
			result: await conn.QuerySingleOrDefaultAsync<ArticleIdsPage>(
				sql: "community_reads.search_articles",
				param: new {
					page_number = pageNumber,
					page_size = pageSize,
					source_slugs = sourceSlugs,
					author_slugs = authorSlugs,
					tag_slugs = tagSlugs,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<long> SetAotd(
			this NpgsqlConnection conn
		) => await conn.QuerySingleOrDefaultAsync<long>(
			sql: "community_reads.set_aotd_v1",
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region core
		public static async Task<api.DataAccess.Models.TimeZone> GetTimeZoneById(
			this NpgsqlConnection conn,
			long id
		) => await conn.QuerySingleOrDefaultAsync<api.DataAccess.Models.TimeZone>(
			sql: "core.get_time_zone_by_id",
			param: new {
				id
			},
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<api.DataAccess.Models.TimeZone> GetTimeZones(
			this NpgsqlConnection conn
		) => conn.Query<api.DataAccess.Models.TimeZone>(
			sql: "get_time_zones",
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region social
		public static async Task<Comment> CreateComment(
			this NpgsqlConnection conn,
			string text,
			long articleId,
			long? parentCommentId,
			long userAccountId,
			ClientAnalytics analytics
		) => await conn.QuerySingleOrDefaultAsync<Comment>(
			sql: "social.create_comment",
			param: new {
				text,
				article_id = articleId,
				parent_comment_id = parentCommentId,
				user_account_id = userAccountId,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Comment> CreateCommentAddendum(
			this NpgsqlConnection conn,
			long commentId,
			string textContent
		) => await conn.QuerySingleOrDefaultAsync<Comment>(
			sql: "social.create_comment_addendum",
			param: new {
				comment_id = commentId,
				text_content = textContent
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Following> CreateFollowing(
			this NpgsqlConnection conn,
			long followerUserId,
			string followeeUserName,
			ClientAnalytics analytics
		) => await conn.QuerySingleAsync<Following>(
			sql: "social.create_following",
			param: new {
				follower_user_id = followerUserId,
				followee_user_name = followeeUserName,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SilentPost> CreateSilentPost(
			this NpgsqlConnection conn,
			long userAccountId,
			long articleId,
			ClientAnalytics analytics
		) => await conn.QuerySingleAsync<SilentPost>(
			sql: "social.create_silent_post",
			param: new {
				user_account_id = userAccountId,
				article_id = articleId,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Comment> DeleteComment(
			this NpgsqlConnection conn,
			long commentId
		) => await conn.QuerySingleOrDefaultAsync<Comment>(
			sql: "social.delete_comment",
			param: new {
				comment_id = commentId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Comment> GetComment(
			this NpgsqlConnection conn,
			long commentId
		) => await conn.QuerySingleOrDefaultAsync<Comment>(
			sql: "social.get_comment",
			param: new {
				comment_id = commentId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<Comment>> GetComments(
			this NpgsqlConnection conn,
			long articleId
		) => await conn.QueryAsync<Comment>(
			sql: "social.get_comments",
			param: new {
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<string>> GetFollowees(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QueryAsync<string>(
			sql: "social.get_followees",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<Follower>> GetFollowers(
			this NpgsqlConnection conn,
			long? viewerUserId,
			string subjectUserName
		) => await conn.QueryAsync<Follower>(
			sql: "social.get_followers",
			param: new {
				viewer_user_id = viewerUserId,
				subject_user_name = subjectUserName
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Following> GetFollowing(
			this NpgsqlConnection conn,
			long? followingId
		) => await conn.QuerySingleOrDefaultAsync<Following>(
			sql: "social.get_following",
			param: new {
				following_id = followingId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<PostReference>> GetNotificationPosts(
			this NpgsqlConnection conn,
			long userId,
			int pageNumber,
			int pageSize
		) => PageResult<PostReference>.Create(
			result: await conn.QuerySingleOrDefaultAsync<PostReferencePage>(
				sql: "social.get_notification_posts_v1",
				param: new {
					user_id = userId,
					page_size = pageSize,
					page_number = pageNumber
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<IEnumerable<Post>> GetPostsAsync(
			this NpgsqlConnection conn,
			PostReference[] postReferences,
			long? userAccountId,
			NotificationEventType[] alertEventTypes
		) => await conn.QueryAsync<Post>(
			sql: "social.get_posts",
			param: new {
				post_references = postReferences,
				user_account_id = userAccountId,
				alert_event_types = alertEventTypes
					.Select(
						eventType => PostgresSerialization.SerializeEnum(eventType)
					)
					.ToArray()
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<PostReference>> GetPostsFromFollowees(
			this NpgsqlConnection conn,
			long userId,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<PostReference>.Create(
			result: await conn.QuerySingleOrDefaultAsync<PostReferencePage>(
				sql: "social.get_posts_from_followees_v1",
				param: new {
					user_id = userId,
					page_size = pageSize,
					page_number = pageNumber,
					min_length = minLength,
					max_length = maxLength
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<PostReference>> GetPostsFromInbox(
			this NpgsqlConnection conn,
			long userId,
			int pageNumber,
			int pageSize
		) => PageResult<PostReference>.Create(
			result: await conn.QuerySingleOrDefaultAsync<PostReferencePage>(
				sql: "get_posts_from_inbox_v1",
				param: new {
					user_id = userId,
					page_size = pageSize,
					page_number = pageNumber
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<PostReference>> GetPostsFromUser(
			this NpgsqlConnection conn,
			string subjectUserName,
			int pageNumber,
			int pageSize
		) => PageResult<PostReference>.Create(
			result: await conn.QuerySingleOrDefaultAsync<PostReferencePage>(
				sql: "social.get_posts_from_user",
				param: new {
					subject_user_name = subjectUserName,
					page_size = pageSize,
					page_number = pageNumber
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<Profile> GetProfile(
			this NpgsqlConnection conn,
			long? viewerUserId,
			string subjectUserName
		) => await conn.QuerySingleAsync<Profile>(
			sql: "social.get_profile",
			param: new {
				viewer_user_id = viewerUserId,
				subject_user_name = subjectUserName
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PageResult<PostReference>> GetReplyPosts(
			this NpgsqlConnection conn,
			long userId,
			int pageNumber,
			int pageSize
		) => PageResult<PostReference>.Create(
			result: await conn.QuerySingleOrDefaultAsync<PostReferencePage>(
				sql: "social.get_reply_posts_v1",
				param: new {
					user_id = userId,
					page_size = pageSize,
					page_number = pageNumber
				},
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<SilentPost> GetSilentPost(
			this NpgsqlConnection conn,
			long id
		) => await conn.QuerySingleOrDefaultAsync<SilentPost>(
			sql: "social.get_silent_post",
			param: new {
				id
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Comment> ReviseComment(
			this NpgsqlConnection conn,
			long commentId,
			string revisedText
		) => await conn.QuerySingleOrDefaultAsync<Comment>(
			sql: "social.revise_comment",
			param: new {
				comment_id = commentId,
				revised_text = revisedText
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task Unfollow(
			this NpgsqlConnection conn,
			long followerUserId,
			string followeeUserName,
			ClientAnalytics analytics
		) => await conn.ExecuteAsync(
			sql: "social.unfollow",
			param: new {
				follower_user_id = followerUserId,
				followee_user_name = followeeUserName,
				analytics = PostgresSerialization.SerializeJson(new { Client = analytics })
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region stats
		public static async Task<IEnumerable<LeaderboardRanking>> GetCurrentStreakLeaderboard(
			this NpgsqlConnection conn,
			long? userAccountId,
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
		public static async Task<IEnumerable<AuthorRanking>> GetAuthorLeaderboard(
			this NpgsqlConnection conn,
			int maxRank,
			DateTime? sinceDate
		) => await conn.QueryAsync<AuthorRanking>(
			sql: "stats.get_top_author_leaderboard",
			param: new {
				max_rank = maxRank,
				since_date = sinceDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<AuthorEarningsRanking>> GetAuthorLeaderboard(
			this NpgsqlConnection conn,
			int minAmountEarned,
			int maxAmountEarned
		) => await conn.QueryAsync<AuthorEarningsRanking>(
			sql: "stats.get_top_author_leaderboard",
			param: new {
				min_amount_earned = minAmountEarned,
				max_amount_earned = maxAmountEarned
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
			DateTime? now = null
		) {
			if (!now.HasValue) {
				now = DateTime.UtcNow;
			}
			return await conn.QuerySingleOrDefaultAsync<UserLeaderboardRankings>(
				sql: "stats.get_user_leaderboard_rankings",
				param: new {
					user_account_id = userAccountId,
					longest_read_since_date = now.Value.Subtract(Leaderboards.LongestReadOffset),
					scout_since_date = now.Value.Subtract(Leaderboards.ScoutOffset),
					scribe_since_date = now.Value.Subtract(Leaderboards.ScribeOffset)
				},
				commandType: CommandType.StoredProcedure
			);
		}
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

		#region subscriptions
		public static async Task<SubscriptionPaymentMethod> AssignDefaultSubscriptionPaymentMethod(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerAccountId,
			string providerPaymentMethodId
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPaymentMethod>(
			sql: "subscriptions.assign_default_payment_method",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_account_id = providerAccountId,
				provider_payment_method_id = providerPaymentMethodId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionAllocationCalculation> CalculateAllocationForAllSubscriptionPeriodsAsync(
			this NpgsqlConnection connection
		) => await connection.QuerySingleAsync<SubscriptionAllocationCalculation>(
			sql: "subscriptions.calculate_allocation_for_all_periods",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionDistributionCalculation> CalculateDistributionForSubscriptionPeriodAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPeriodId
		) => await connection.QuerySingleAsync<SubscriptionDistributionCalculation>(
			sql: "subscriptions.calculate_distribution_for_period",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_period_id = providerPeriodId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthorPayout> CreateAuthorPayoutAsync(
			this NpgsqlConnection connection,
			string id,
			DateTime dateCreated,
			string payoutAccountId,
			int amount
		) => await connection.QuerySingleOrDefaultAsync<AuthorPayout>(
			sql: "subscriptions.create_author_payout",
			param: new {
				id,
				date_created = dateCreated,
				payout_account_id = payoutAccountId,
				amount
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPriceLevel> CreateCustomSubscriptionPriceLevelAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPriceId,
			DateTime dateCreated,
			int amount
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPriceLevel>(
			sql: "subscriptions.create_custom_price_level",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_price_id = providerPriceId,
				date_created = dateCreated,
				amount
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPeriodDistribution> CreateDistributionForSubscriptionPeriodAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPeriodId
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPeriodDistribution>(
			sql: "subscriptions.create_distribution_for_period",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_period_id = providerPeriodId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<SubscriptionPeriodDistribution>> CreateDistributionsForLapsedSubscriptionPeriodsAsync(
			this NpgsqlConnection connection,
			long? userAccountId
		) => await connection.QueryAsync<SubscriptionPeriodDistribution>(
			sql: "subscriptions.create_distributions_for_lapsed_periods",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionAccount> CreateOrUpdateSubscriptionAccountAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerAccountId,
			long? userAccountId,
			DateTime dateCreated,
			SubscriptionEnvironment environment
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionAccount>(
			sql: "subscriptions.create_or_update_subscription_account",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_account_id = providerAccountId,
				user_account_id = userAccountId,
				date_created = dateCreated,
				environment = PostgresSerialization.SerializeEnum(environment)
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<Subscription> CreateOrUpdateSubscriptionAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerSubscriptionId,
			string providerAccountId,
			DateTime dateCreated,
			string latestReceipt
		) => await connection.QuerySingleOrDefaultAsync<Subscription>(
			sql: "subscriptions.create_or_update_subscription",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_subscription_id = providerSubscriptionId,
				provider_account_id = providerAccountId,
				date_created = dateCreated,
				latest_receipt = latestReceipt
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPeriod> CreateOrUpdateSubscriptionPeriodAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPeriodId,
			string providerSubscriptionId,
			string providerPriceId,
			string providerPaymentMethodId,
			DateTime beginDate,
			DateTime endDate,
			DateTime dateCreated,
			SubscriptionPaymentStatus paymentStatus,
			DateTime? datePaid,
			DateTime? dateRefunded,
			string refundReason,
			int? prorationDiscount
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPeriod>(
			sql: "subscriptions.create_or_update_subscription_period",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_period_id = providerPeriodId,
				provider_subscription_id = providerSubscriptionId,
				provider_price_id = providerPriceId,
				provider_payment_method_id = providerPaymentMethodId,
				begin_date = beginDate,
				end_date = endDate,
				date_created = dateCreated,
				payment_status = PostgresSerialization.SerializeEnum(paymentStatus),
				date_paid = datePaid,
				date_refunded = dateRefunded,
				refund_reason = refundReason,
				proration_discount = prorationDiscount
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PayoutAccount> CreatePayoutAccountAsync(
			this NpgsqlConnection connection,
			string id,
			long userAccountId
		) => await connection.QuerySingleOrDefaultAsync<PayoutAccount>(
			sql: "subscriptions.create_payout_account",
			param: new {
				id,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPaymentMethod> CreateSubscriptionPaymentMethodAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPaymentMethodId,
			string providerAccountId,
			DateTime dateCreated,
			SubscriptionPaymentMethodWallet wallet,
			SubscriptionPaymentMethodBrand brand,
			string lastFourDigits,
			string country,
			int expirationMonth,
			int expirationYear
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPaymentMethod>(
			sql: "subscriptions.create_payment_method",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_payment_method_id = providerPaymentMethodId,
				provider_account_id = providerAccountId,
				date_created = dateCreated,
				wallet = PostgresSerialization.SerializeEnum(wallet),
				brand = PostgresSerialization.SerializeEnum(brand),
				last_four_digits = lastFourDigits,
				country,
				expiration_month = expirationMonth,
				expiration_year = expirationYear
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionRenewalStatusChange> CreateSubscriptionRenewalStatusChangeAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerSubscriptionId,
			DateTime dateCreated,
			bool autoRenewEnabled,
			string providerPriceId,
			string expirationIntent
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionRenewalStatusChange>(
			sql: "subscriptions.create_subscription_renewal_status_change",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_subscription_id = providerSubscriptionId,
				date_created = dateCreated,
				auto_renew_enabled = autoRenewEnabled,
				provider_price_id = providerPriceId,
				expiration_intent = expirationIntent
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionStatus> GetCurrentSubscriptionStatusForUserAccountAsync(
			this NpgsqlConnection connection,
			long userAccountId
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionStatus>(
			sql: "subscriptions.get_current_subscription_status_for_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPriceLevel> GetCustomSubscriptionPriceLevelForProviderAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			int amount
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPriceLevel>(
			sql: "subscriptions.get_custom_price_level_for_provider",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				amount
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPaymentMethod> GetDefaultPaymentMethodForSubscriptionAccountAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerAccountId
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPaymentMethod>(
			sql: "subscriptions.get_default_payment_method_for_subscription_account",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_account_id = providerAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<DonationRecipient> GetDonationRecipientForAuthorAsync(
			this NpgsqlConnection connection,
			long authorId
		) => await connection.QuerySingleOrDefaultAsync<DonationRecipient>(
			sql: "subscriptions.get_donation_recipient_for_author",
			param: new {
				author_id = authorId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<FreeTrialArticleView>> GetFreeArticleViewsForUserAccountAsync(
			this NpgsqlConnection connection,
			long userAccountId
		) => await connection.QueryAsync<FreeTrialArticleView>(
			sql: "subscriptions.get_free_article_views_for_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<FreeTrialCredit>> GetFreeTrialCreditsForUserAccountAsync(
			this NpgsqlConnection connection,
			long userAccountId
		) => await connection.QueryAsync<FreeTrialCredit>(
			sql: "subscriptions.get_free_trial_credits_for_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PayoutAccount> GetPayoutAccountAsync(
			this NpgsqlConnection connection,
			string id
		) => await connection.QuerySingleOrDefaultAsync<PayoutAccount>(
			sql: "subscriptions.get_payout_account",
			param: new {
				id
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PayoutAccount> GetPayoutAccountForUserAccountAsync(
			this NpgsqlConnection connection,
			long userAccountId
		) => await connection.QuerySingleOrDefaultAsync<PayoutAccount>(
			sql: "subscriptions.get_payout_account_for_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<SubscriptionPriceLevel>> GetStandardSubscriptionPriceLevelsForProviderAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider
		) => await connection.QueryAsync<SubscriptionPriceLevel>(
			sql: "subscriptions.get_standard_price_levels_for_provider",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider)
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPaymentMethod> GetSubscriptionPaymentMethodAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPaymentMethodId
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPaymentMethod>(
			sql: "subscriptions.get_payment_method",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_payment_method_id = providerPaymentMethodId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPeriod> GetSubscriptionPeriodAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPeriodId
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPeriod>(
			sql: "subscriptions.get_subscription_period",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_period_id = providerPeriodId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<SubscriptionAccount>> GetSubscriptionAccountsForUserAccountAsync(
			this NpgsqlConnection connection,
			long userAccountId
		) => await connection.QueryAsync<SubscriptionAccount>(
			sql: "subscriptions.get_subscription_accounts_for_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionStatus> GetSubscriptionStatusForSubscriptionAccountAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerAccountId
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionStatus>(
			sql: "subscriptions.get_subscription_status_for_subscription_account",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_account_id = providerAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<SubscriptionStatus>> GetSubscriptionStatusesForUserAccountAsync(
			this NpgsqlConnection connection,
			long userAccountId
		) => await connection.QueryAsync<SubscriptionStatus>(
			sql: "subscriptions.get_subscription_statuses_for_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionDistributionAuthorReport> RunAuthorDistributionReportForSubscriptionPeriodDistributionsAsync(
			this NpgsqlConnection connection,
			long authorId
		) => await connection.QuerySingleAsync<SubscriptionDistributionAuthorReport>(
			sql: "subscriptions.run_author_distribution_report_for_period_distributions",
			param: new {
				author_id = authorId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<AuthorEarningsReportLineItem>> RunAuthorsEarningsReportAsync(
			this NpgsqlConnection connection,
			int minAmountEarned,
			int maxAmountEarned
		) => await connection.QueryAsync<AuthorEarningsReportLineItem>(
			sql: "subscriptions.run_authors_earnings_report",
			param: new {
				min_amount_earned = minAmountEarned,
				max_amount_earned = maxAmountEarned
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionDistributionReport> RunDistributionReportForSubscriptionPeriodCalculationAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPeriodId
		) => await connection.QuerySingleAsync<SubscriptionDistributionReport>(
			sql: "subscriptions.run_distribution_report_for_period_calculation",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_period_id = providerPeriodId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionDistributionReport> RunDistributionReportForSubscriptionPeriodDistributionsAsync(
			this NpgsqlConnection connection,
			long userAccountId
		) => await connection.QuerySingleAsync<SubscriptionDistributionReport>(
			sql: "subscriptions.run_distribution_report_for_period_distributions",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PayoutTotalsReport> RunPayoutTotalsReportAsync(
			this NpgsqlConnection connection
		) => await connection.QuerySingleOrDefaultAsync<PayoutTotalsReport>(
			sql: "subscriptions.run_payout_totals_report",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PayoutTotalsReport> RunPayoutTotalsReportForUserAccountAsync(
			this NpgsqlConnection connection,
			long userAccountId
		) => await connection.QuerySingleOrDefaultAsync<PayoutTotalsReport>(
			sql: "subscriptions.run_payout_totals_report_for_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<PayoutAccount> UpdatePayoutAccountAsync(
			this NpgsqlConnection connection,
			string id,
			DateTime? dateDetailsSubmitted,
			DateTime? datePayoutsEnabled
		) => await connection.QuerySingleOrDefaultAsync<PayoutAccount>(
			sql: "subscriptions.update_payout_account",
			param: new {
				id,
				date_details_submitted = dateDetailsSubmitted,
				date_payouts_enabled = datePayoutsEnabled
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SubscriptionPaymentMethod> UpdateSubscriptionPaymentMethodAsync(
			this NpgsqlConnection connection,
			SubscriptionProvider provider,
			string providerPaymentMethodId,
			SubscriptionEventSource eventSource,
			int expirationMonth,
			int expirationYear
		) => await connection.QuerySingleOrDefaultAsync<SubscriptionPaymentMethod>(
			sql: "subscriptions.update_payment_method",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_payment_method_id = providerPaymentMethodId,
				event_source = PostgresSerialization.SerializeEnum(eventSource),
				expiration_month = expirationMonth,
				expiration_year = expirationYear
			},
			commandType: CommandType.StoredProcedure
		);
		#endregion

		#region user_account_api
		public static async Task<AuthServiceAccount> AssociateAuthServiceAccount(
			this NpgsqlConnection conn,
			long identityId,
			long authenticationId,
			long userAccountId,
			AuthServiceAssociationMethod associationMethod
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceAccount>(
			sql: "user_account_api.associate_auth_service_account",
			param: new {
				identity_id = identityId,
				authentication_id = authenticationId,
				user_account_id = userAccountId,
				association_method = PostgresSerialization.SerializeEnum(associationMethod)
			},
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
		public static async Task<AuthServiceRequestToken> CancelAuthServiceRequestToken(
			this NpgsqlConnection conn,
			string tokenValue
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceRequestToken>(
			sql: "user_account_api.cancel_auth_service_request_token",
			param: new {
				token_value = tokenValue
			},
			commandType: CommandType.StoredProcedure
		);
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
		public static async Task<AuthServiceAuthentication> CreateAuthServiceAuthentication(
			this NpgsqlConnection conn,
			long identityId,
			string sessionId
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceAuthentication>(
			sql: "user_account_api.create_auth_service_authentication",
			param: new {
				identity_id = identityId,
				session_id = sessionId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceAccount> CreateAuthServiceIdentity(
			this NpgsqlConnection conn,
			AuthServiceProvider provider,
			string providerUserId,
			string providerUserEmailAddress,
			bool isEmailAddressPrivate,
			string providerUserName,
			string providerUserHandle,
			AuthServiceRealUserRating? realUserRating,
			UserAccountCreationAnalytics signUpAnalytics
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceAccount>(
			sql: "user_account_api.create_auth_service_identity",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_user_id = providerUserId,
				provider_user_email_address = providerUserEmailAddress,
				is_email_address_private = isEmailAddressPrivate,
				provider_user_name = providerUserName,
				provider_user_handle = providerUserHandle,
				real_user_rating = PostgresSerialization.SerializeEnum(realUserRating),
				sign_up_analytics = signUpAnalytics != null ?
					PostgresSerialization.SerializeJson(signUpAnalytics) :
					null
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServicePost> CreateAuthServicePost(
			this NpgsqlConnection conn,
			long identityId,
			long? commentId,
			long? silentPostId,
			string content,
			string providerPostId
		) => await conn.QuerySingleOrDefaultAsync<AuthServicePost>(
			sql: "user_account_api.create_auth_service_post",
			param: new {
				identity_id = identityId,
				comment_id = commentId,
				silent_post_id = silentPostId,
				content,
				provider_post_id = providerPostId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceRefreshToken> CreateAuthServiceRefreshToken(
			this NpgsqlConnection conn,
			long identityId,
			string rawValue
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceRefreshToken>(
			sql: "user_account_api.create_auth_service_refresh_token",
			param: new {
				identity_id = identityId,
				raw_value = rawValue
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceRequestToken> CreateAuthServiceRequestToken(
			this NpgsqlConnection conn,
			AuthServiceProvider provider,
			string tokenValue,
			string tokenSecret,
			UserAccountCreationAnalytics signUpAnalytics
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceRequestToken>(
			sql: "user_account_api.create_auth_service_request_token",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				token_value = tokenValue,
				token_secret = tokenSecret,
				sign_up_analytics = signUpAnalytics != null ?
					PostgresSerialization.SerializeJson(signUpAnalytics) :
					null
			},
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
		public static async Task<PasswordResetRequest> CreatePasswordResetRequest(
			this NpgsqlConnection conn,
			long userAccountId,
			long? authServiceAuthenticationId
		) => await conn.QuerySingleOrDefaultAsync<PasswordResetRequest>(
			sql: "user_account_api.create_password_reset_request",
			param: new {
				user_account_id = userAccountId,
				auth_service_authentication_id = authServiceAuthenticationId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<ProvisionalUserAccount> CreateProvisionalUserAccount(
			this NpgsqlConnection conn,
			UserAccountProvisionalCreationAnalytics analytics
		) => await conn.QuerySingleOrDefaultAsync<ProvisionalUserAccount>(
			sql: "user_account_api.create_provisional_user_account",
			param: new {
				analytics = PostgresSerialization.SerializeJson(analytics)
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserAccount> CreateUserAccount(
			this NpgsqlConnection conn,
			string name,
			string email,
			byte[] passwordHash,
			byte[] passwordSalt,
			long timeZoneId,
			DisplayTheme? theme,
			UserAccountCreationAnalytics analytics
		) => await CreateUserAccount(
			conn,
			name,
			email,
			passwordHash,
			passwordSalt,
			timeZoneId,
			theme,
			PostgresSerialization.SerializeJson(analytics)
		);
		public static async Task<UserAccount> CreateUserAccount(
			this NpgsqlConnection conn,
			string name,
			string email,
			byte[] passwordHash,
			byte[] passwordSalt,
			long timeZoneId,
			DisplayTheme? theme,
			string analytics
		) {
			try {
				return await conn.QuerySingleOrDefaultAsync<UserAccount>(
					sql: "user_account_api.create_user_account",
					param: new {
						name,
						email,
						password_hash = passwordHash,
						password_salt = passwordSalt,
						time_zone_id = timeZoneId,
						theme = PostgresSerialization.SerializeEnum(theme),
						analytics
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
		public static async Task<UserAccount> DeleteUserAccountAsync(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QuerySingleOrDefaultAsync<UserAccount>(
			sql: "user_account_api.delete_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceAccount> DisassociateAuthServiceAccount(
			this NpgsqlConnection conn,
			long identityId
		) => await conn.QuerySingleOrDefaultAsync(
			sql: "user_account_api.disassociate_auth_service_account",
			param: new {
				identity_id = identityId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceAccount> GetAuthServiceAccountByIdentityId(
			this NpgsqlConnection conn,
			long identityId
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceAccount>(
			sql: "user_account_api.get_auth_service_account_by_identity_id",
			param: new {
				identity_id = identityId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceAccount> GetAuthServiceAccountByProviderUserId(
			this NpgsqlConnection conn,
			AuthServiceProvider provider,
			string providerUserId
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceAccount>(
			sql: "user_account_api.get_auth_service_account_by_provider_user_id",
			param: new {
				provider = PostgresSerialization.SerializeEnum(provider),
				provider_user_id = providerUserId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<IEnumerable<AuthServiceAccount>> GetAuthServiceAccountsForUserAccount(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QueryAsync<AuthServiceAccount>(
			sql: "user_account_api.get_auth_service_accounts_for_user_account",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceAuthentication> GetAuthServiceAuthenticationById(
			this NpgsqlConnection conn,
			long authenticationId
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceAuthentication>(
			sql: "user_account_api.get_auth_service_authentication_by_id",
			param: new {
				authentication_id = authenticationId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceRequestToken> GetAuthServiceRequestToken(
			this NpgsqlConnection conn,
			string tokenValue
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceRequestToken>(
			sql: "user_account_api.get_auth_service_request_token",
			param: new {
				token_value = tokenValue
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<DisplayPreference> GetDisplayPreference(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QuerySingleOrDefaultAsync<DisplayPreference>(
			sql: "user_account_api.get_display_preference",
			param: new {
				user_account_id = userAccountId
			},
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
		public static PasswordResetRequest GetPasswordResetRequest(this NpgsqlConnection conn, long passwordResetRequestId) => conn.QuerySingleOrDefault<PasswordResetRequest>(
			sql: "user_account_api.get_password_reset_request",
			param: new { password_reset_request_id = passwordResetRequestId },
			commandType: CommandType.StoredProcedure
		);
		public static UserAccount GetUserAccountByEmail(this NpgsqlConnection conn, string email) => conn.QuerySingleOrDefault<UserAccount>(
			sql: "user_account_api.get_user_account_by_email",
			param: new { email },
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserAccount> GetUserAccountById(this NpgsqlConnection conn, long userAccountId) => await conn.QuerySingleOrDefaultAsync<UserAccount>(
			sql: "user_account_api.get_user_account_by_id",
			param: new { user_account_id = userAccountId },
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserAccount> GetUserAccountByName(
			this NpgsqlConnection conn,
			string userName
		) => await conn.QuerySingleAsync<UserAccount>(
			sql: "user_account_api.get_user_account_by_name",
			param: new {
				user_name = userName
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<long> GetUserAccountIdByName(
			this NpgsqlConnection conn,
			string userName
		) => await conn.QuerySingleAsync<long>(
			sql: "user_account_api.get_user_account_id_by_name",
			param: new {
				user_name = userName
			},
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
		public static IEnumerable<UserAccount> GetUserAccounts(this NpgsqlConnection conn) => conn.Query<UserAccount>(
			sql: "user_account_api.get_user_accounts",
			commandType: CommandType.StoredProcedure
		);
		public static async Task<ProvisionalUserAccount> MergeProvisionalUserAccount(
			this NpgsqlConnection conn,
			long provisionalUserAccountId,
			long userAccountId
		) => await conn.QuerySingleAsync<ProvisionalUserAccount>(
			sql: "user_account_api.merge_provisional_user_account",
			param: new {
				provisional_user_account_id = provisionalUserAccountId,
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<UserAccount> RegisterOrientationCompletion(
			this NpgsqlConnection conn,
			long userAccountId
		) => await conn.QuerySingleOrDefaultAsync<UserAccount>(
			sql: "user_account_api.register_orientation_completion",
			param: new {
				user_account_id = userAccountId
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<DisplayPreference> SetDisplayPreference(
			this NpgsqlConnection conn,
			long userAccountId,
			DisplayTheme theme,
			int textSize,
			bool hideLinks
		) => await conn.QuerySingleAsync<DisplayPreference>(
			sql: "user_account_api.set_display_preference",
			param: new {
				user_account_id = userAccountId,
				theme = PostgresSerialization.SerializeEnum(theme),
				text_size = textSize,
				hide_links = hideLinks
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceAccessToken> StoreAuthServiceAccessToken(
			this NpgsqlConnection conn,
			long identityId,
			long requestId,
			string tokenValue,
			string tokenSecret
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceAccessToken>(
			sql: "user_account_api.store_auth_service_access_token",
			param: new {
				identity_id = identityId,
				request_id = requestId,
				token_value = tokenValue,
				token_secret = tokenSecret
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<AuthServiceAccount> UpdateAuthServiceAccountUser(
			this NpgsqlConnection conn,
			long identityId,
			string emailAddress,
			bool isEmailAddressPrivate,
			string name,
			string handle
		) => await conn.QuerySingleOrDefaultAsync<AuthServiceAccount>(
			sql: "user_account_api.update_auth_service_account_user",
			param: new {
				identity_id = identityId,
				email_address = emailAddress,
				is_email_address_private = isEmailAddressPrivate,
				name,
				handle
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