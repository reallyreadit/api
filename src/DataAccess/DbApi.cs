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

namespace api.DataAccess {
    public static class DbApi {
		private static string ConvertEnumToString(Enum value) => (
			Regex
				.Replace(
					input: value.ToString(),
					pattern: "([a-z])?([A-Z])",
					evaluator: match => (
							match.Groups[1].Success ?
								match.Groups[1].Value + "_" :
								String.Empty
						) +
						match.Groups[2].Value.ToLower()
				)
		);
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
		public static Task<IEnumerable<UserAccountCreation>> GetUserAccountCreations(
			this NpgsqlConnection conn,
			DateTime startDate,
			DateTime endDate
		) => conn.QueryAsync<UserAccountCreation>(
			sql: "analytics.get_user_account_creations",
			param: new {
				start_date = startDate,
				end_date = endDate
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task LogClientErrorReport(
			this NpgsqlConnection conn,
			string content,
			RequestAnalytics analytics
		) => conn.ExecuteAsync(
			sql: "analytics.log_client_error_report",
			param: new {
				content,
				analytics = PostgresJsonSerialization.Serialize(analytics)
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
		public static async Task<Comment> CreateComment(
			this NpgsqlConnection conn,
			string text,
			long articleId,
			long? parentCommentId,
			long userAccountId,
			RequestAnalytics analytics
		) => await conn.QuerySingleOrDefaultAsync<Comment>(
			sql: "article_api.create_comment",
			param: new {
				text,
				article_id = articleId,
				parent_comment_id = parentCommentId,
				user_account_id = userAccountId,
				analytics = PostgresJsonSerialization.Serialize(analytics)
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
				analytics = PostgresJsonSerialization.Serialize(analytics)
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
			long? userAccountId = null
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
		public static async Task<Comment> GetComment(this NpgsqlConnection conn, long commentId) => await conn.QuerySingleOrDefaultAsync<Comment>(
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
				analytics = PostgresJsonSerialization.Serialize(analytics)
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

		#region notifications
		public static async Task<IEnumerable<NotificationReceipt>> ClearAllAlerts(
			this NpgsqlConnection conn,
			NotificationEventType type,
			long userAccountId
		) => await conn.QueryAsync<NotificationReceipt>(
			sql: "notifications.clear_all_alerts",
			param: new {
				type = ConvertEnumToString(type),
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
		public static async Task<IEnumerable<NotificationDispatch>> CreateAotdNotifications(
			this NpgsqlConnection conn,
			long articleId
		) => await conn.QueryAsync<NotificationDispatch>(
			sql: "notifications.create_aotd_notifications",
			param: new {
				article_id = articleId
			},
			commandType: CommandType.StoredProcedure
		);
		public static long CreateBulkMailing(
			this NpgsqlConnection conn,
			string subject,
			string body,
			string type,
			long userAccountId,
			long[] recipientIds
		) => conn.QuerySingleOrDefault<long>(
			sql: "notifications.create_bulk_mailing",
			param: new {
				subject, body, type,
				user_account_id = userAccountId,
				recipient_ids = recipientIds
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
			sql: "notifications.create_email_notification",
			param: new {
				notification_type = notificationType,
				mail = PostgresJsonSerialization.Serialize(mail),
				bounce = PostgresJsonSerialization.Serialize(bounce),
				complaint = PostgresJsonSerialization.Serialize(complaint)
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task<NotificationDispatch> CreateFollowerNotification(
			this NpgsqlConnection conn,
			long followingId,
			long followerId,
			long followeeId
		) => conn.QuerySingleOrDefaultAsync<NotificationDispatch>(
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
				channel = ConvertEnumToString(channel),
				action = ConvertEnumToString(action),
				url,
				reply_id = replyId
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task<IEnumerable<NotificationDispatch>> CreateLoopbackNotifications(
			this NpgsqlConnection conn,
			long articleId,
			long commentId,
			long commentAuthorId
		) => conn.QueryAsync<NotificationDispatch>(
			sql: "notifications.create_loopback_notifications",
			param: new {
				article_id = articleId,
				comment_id = commentId,
				comment_author_id = commentAuthorId
			},
			commandType: CommandType.StoredProcedure
		);
		public static Task<IEnumerable<NotificationDispatch>> CreatePostNotifications(
			this NpgsqlConnection conn,
			long posterId,
			long? commentId,
			long? silentPostId
		) => conn.QueryAsync<NotificationDispatch>(
			sql: "notifications.create_post_notifications",
			param: new {
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
		public static Task<NotificationDispatch> CreateReplyNotification(
			this NpgsqlConnection conn,
			long replyId,
			long replyAuthorId,
			long parentId
		) => conn.QuerySingleOrDefaultAsync<NotificationDispatch>(
			sql: "notifications.create_reply_notification",
			param: new {
				reply_id = replyId,
				reply_author_id = replyAuthorId,
				parent_id = parentId
			},
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<string> GetBlockedEmailAddresses(this NpgsqlConnection conn) => conn.Query<string>(
			sql: "notifications.get_blocked_email_addresses",
			commandType: CommandType.StoredProcedure
		);
		public static IEnumerable<UserAccount> GetConfirmationReminderRecipients(this NpgsqlConnection conn) => conn.Query<UserAccount>(
			sql: "notifications.get_confirmation_reminder_recipients",
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
				suggested_reading_via_email = options.SuggestedReadingViaEmail,
				aotd_via_email = options.AotdViaEmail,
				aotd_via_extension = options.AotdViaExtension,
				aotd_via_push = options.AotdViaPush,
				reply_via_email = options.ReplyViaEmail,
				reply_via_extension = options.ReplyViaExtension,
				reply_via_push = options.ReplyViaPush,
				reply_digest_via_email = ConvertEnumToString(options.ReplyDigestViaEmail),
				loopback_via_email = options.LoopbackViaEmail,
				loopback_via_extension = options.LoopbackViaExtension,
				loopback_via_push = options.LoopbackViaPush,
				loopback_digest_via_email = ConvertEnumToString(options.LoopbackDigestViaEmail),
				post_via_email = options.PostViaEmail,
				post_via_extension = options.PostViaExtension,
				post_via_push = options.PostViaPush,
				post_digest_via_email = ConvertEnumToString(options.PostDigestViaEmail),
				follower_via_email = options.FollowerViaEmail,
				follower_via_extension = options.FollowerViaExtension,
				follower_via_push = options.FollowerViaPush,
				follower_digest_via_email = ConvertEnumToString(options.FollowerDigestViaEmail)
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
				reason = ConvertEnumToString(reason)
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
				reason = ConvertEnumToString(reason)
			},
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
		public static async Task<Article> SetAotd(
			this NpgsqlConnection conn
		) => await conn.QuerySingleOrDefaultAsync<Article>(
			sql: "community_reads.set_aotd",
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
		public static async Task<Following> CreateFollowing(
			this NpgsqlConnection conn,
			long followerUserId,
			string followeeUserName,
			RequestAnalytics analytics
		) => await conn.QuerySingleAsync<Following>(
			sql: "social.create_following",
			param: new {
				follower_user_id = followerUserId,
				followee_user_name = followeeUserName,
				analytics = PostgresJsonSerialization.Serialize(analytics)
			},
			commandType: CommandType.StoredProcedure
		);
		public static async Task<SilentPost> CreateSilentPost(
			this NpgsqlConnection conn,
			long userAccountId,
			long articleId,
			RequestAnalytics analytics
		) => await conn.QuerySingleAsync<SilentPost>(
			sql: "social.create_silent_post",
			param: new {
				user_account_id = userAccountId,
				article_id = articleId,
				analytics = PostgresJsonSerialization.Serialize(analytics)
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
		public static async Task<PageResult<ArticlePostMultiMap>> GetPostsFromFollowees(
			this NpgsqlConnection conn,
			long userId,
			int pageNumber,
			int pageSize,
			int? minLength,
			int? maxLength
		) => PageResult<ArticlePostMultiMap>.Create(
			items: await conn.QueryAsync<Article, Post, long, ArticlePostMultiMapPageResult>(
				sql: "social.get_posts_from_followees",
				map: (article, post, totalCount) => new ArticlePostMultiMapPageResult(article, post, totalCount),
				param: new {
					user_id = userId,
					page_size = pageSize,
					page_number = pageNumber,
					min_length = minLength,
					max_length = maxLength
				},
				splitOn: "post_date_created,total_count",
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<ArticlePostMultiMap>> GetPostsFromInbox(
			this NpgsqlConnection conn,
			long userId,
			int pageNumber,
			int pageSize
		) => PageResult<ArticlePostMultiMap>.Create(
			items: await conn.QueryAsync<Article, Post, long, ArticlePostMultiMapPageResult>(
				sql: "social.get_posts_from_inbox",
				map: (article, post, totalCount) => new ArticlePostMultiMapPageResult(article, post, totalCount),
				param: new {
					user_id = userId,
					page_size = pageSize,
					page_number = pageNumber
				},
				splitOn: "post_date_created,total_count",
				commandType: CommandType.StoredProcedure
			),
			pageNumber: pageNumber,
			pageSize: pageSize
		);
		public static async Task<PageResult<ArticlePostMultiMap>> GetPostsFromUser(
			this NpgsqlConnection conn,
			long? viewerUserId,
			string subjectUserName,
			int pageNumber,
			int pageSize
		) => PageResult<ArticlePostMultiMap>.Create(
			items: await conn.QueryAsync<Article, Post, long, ArticlePostMultiMapPageResult>(
				sql: "social.get_posts_from_user",
				map: (article, post, totalCount) => new ArticlePostMultiMapPageResult(article, post, totalCount),
				param: new {
					viewer_user_id = viewerUserId,
					subject_user_name = subjectUserName,
					page_size = pageSize,
					page_number = pageNumber
				},
				splitOn: "post_date_created,total_count",
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
		public static async Task Unfollow(
			this NpgsqlConnection conn,
			long followerUserId,
			string followeeUserName,
			RequestAnalytics analytics
		) => await conn.ExecuteAsync(
			sql: "social.unfollow",
			param: new {
				follower_user_id = followerUserId,
				followee_user_name = followeeUserName,
				analytics = PostgresJsonSerialization.Serialize(analytics)
			},
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

		#region user_account_api
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
			UserAccountCreationAnalytics analytics
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
						analytics = PostgresJsonSerialization.Serialize(analytics)
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