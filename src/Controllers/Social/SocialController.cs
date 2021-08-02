using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using api.DataAccess;
using api.Authentication;
using api.Analytics;
using api.DataAccess.Models;
using System;
using api.DataAccess.Stats;
using api.Encryption;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using api.Notifications;
using api.Commenting;
using api.ReadingVerification;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using api.Controllers.Shared;

namespace api.Controllers.Social {
	public class SocialController : Controller {
		private DatabaseOptions dbOpts;
		private ILogger<SocialController> logger;
		public SocialController(
			IOptions<DatabaseOptions> dbOpts,
			[FromServices] ILogger<SocialController> logger
		) {
			this.dbOpts = dbOpts.Value;
			this.logger = logger;
		}
		[HttpPost]
		public async Task<IActionResult> Comment(
			[FromBody] CommentForm form,
			[FromServices] CommentingService commentingService,
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] ReadingVerificationService verificationService
		) {
			if (String.IsNullOrWhiteSpace(form.Text)) {
				return BadRequest();
			}
			var userAccountId = this.User.GetUserAccountId();
			// First verify that the article has been read and that the comment text is valid.
			Article article;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				article = await db.GetArticleById(form.ArticleId, userAccountId);
				if (!article.IsRead || !commentingService.IsCommentTextValid(form.Text)) {
					return BadRequest();
				}
			}
			// Then post the reply.
			var reply = await commentingService.PostReply(
				text: form.Text,
				articleId: form.ArticleId,
				parentCommentId: obfuscationService.DecodeSingle(form.ParentCommentId).Value,
				userAccountId: userAccountId,
				analytics: this.GetClientAnalytics()
			);
			// Get the leaderboard rankings and update the article afterwards in case this reply changed either.
			IEnumerable<Author> articleAuthors;
			UserLeaderboardRankings userLeaderboardRankings;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				articleAuthors = await db.GetAuthorsOfArticle(
					articleId: form.ArticleId
				);
				userLeaderboardRankings = await db.GetUserLeaderboardRankings(
					userAccountId: userAccountId
				);
				article = await db.GetArticleById(form.ArticleId, userAccountId);
			}
			// Create the client model and return.
			var commentThread = new CommentThread(
				comment: reply,
				badge: userLeaderboardRankings.GetBadge(),
				isAuthor: articleAuthors.Any(
					author => author.UserAccountId == userAccountId
				),
				obfuscationService: obfuscationService
			);
			if (
				this.ClientVersionIsGreaterThanOrEqualTo(new Dictionary<ClientType, SemanticVersion>() {
					{ ClientType.WebAppClient, new SemanticVersion("1.0.0") },
					{ ClientType.WebExtension, new SemanticVersion("1.0.0") },
					{ ClientType.IosApp, new SemanticVersion("3.1.1") },
					{ ClientType.WebEmbed, new SemanticVersion("1.0.0") }
				})
			) {
				return Json(new {
					Article = verificationService.AssignProofToken(
						article: article,
						userAccountId: userAccountId
					),
					Comment = commentThread
				});
			}
			return Json(commentThread);
		}
		[HttpPost]
		public async Task<IActionResult> CommentAddendum(
			[FromBody] CommentAddendumForm form,
			[FromServices] CommentingService commentingService,
			[FromServices] ObfuscationService obfuscationService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var comment = await db.GetComment(obfuscationService.DecodeSingle(form.CommentId).Value);
				if (comment.UserAccountId == User.GetUserAccountId()) {
					var articleAuthors = await db.GetAuthorsOfArticle(
						articleId: comment.ArticleId
					);
					return Json(
						new CommentThread(
							comment: await commentingService.CreateAddendum(comment.Id, form.Text),
							badge: (await db.GetUserLeaderboardRankings(User.GetUserAccountId())).GetBadge(),
							isAuthor: articleAuthors.Any(
								author => author.UserAccountId == comment.UserAccountId
							),
							obfuscationService: obfuscationService
						)
					);
				}
			}
			return BadRequest();
		}
		[HttpPost]
		public async Task<IActionResult> CommentRevision(
			[FromBody] CommentRevisionForm form,
			[FromServices] CommentingService commentingService,
			[FromServices] ObfuscationService obfuscationService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var comment = await db.GetComment(obfuscationService.DecodeSingle(form.CommentId).Value);
				if (
					comment.UserAccountId == User.GetUserAccountId() &&
					commentingService.CanReviseComment(comment)
				) {
					var articleAuthors = await db.GetAuthorsOfArticle(
						articleId: comment.ArticleId
					);
					return Json(
						new CommentThread(
							comment: await commentingService.ReviseComment(comment.Id, form.Text),
							badge: (await db.GetUserLeaderboardRankings(User.GetUserAccountId())).GetBadge(),
							isAuthor: articleAuthors.Any(
								author => author.UserAccountId == comment.UserAccountId
							),
							obfuscationService: obfuscationService
						)
					);
				}
			}
			return BadRequest();
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Comments(
			[FromServices] IMemoryCache memoryCache,
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] ReadingVerificationService verificationService,
			[FromQuery] CommentsQuery query
		) {
			var userAccountId = User.GetUserAccountIdOrDefault();
			CommentThread[] comments;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var leaderboards = await memoryCache.GetOrCreateAsync<Leaderboards>(
					"Leaderboards",
					async entry => {
						logger.LogInformation("Caching Leaderboards");
						entry.SetAbsoluteExpiration(
							TimeSpan.FromMinutes(1)
						);
						return await db.GetLeaderboards(
							userAccountId: userAccountId,
							now: DateTime.UtcNow
						);
					}
				);
				var article = await db.GetArticleBySlug(query.Slug, userAccountId);
				if (article == null) {
					logger.LogError("Article lookup failed. Slug: {Slug}", query.Slug);
					return BadRequest(
						new[] { "Article not found." }
					);
				}
				var articleAuthors = await db.GetAuthorsOfArticle(
					articleId: article.Id
				);
				comments = (
						await db.GetComments(
							articleId: article.Id
						)
					)
					.Select(
						comment => new CommentThread(
							comment: comment,
							badge: leaderboards.GetBadge(comment.UserAccount),
							isAuthor: articleAuthors.Any(
								author => author.UserAccountId == comment.UserAccountId
							),
							obfuscationService: obfuscationService
						)
					)
					.ToArray();
			}
			foreach (var comment in comments.Where(c => c.ParentCommentId != null)) {
				comments.Single(c => c.Id == comment.ParentCommentId).Children.Add(comment);
			}
			foreach (var comment in comments) {
				comment.Children.Sort(
					(a, b) => a.IsAuthor && !b.IsAuthor ?
						-1 :
						b.MaxDate.CompareTo(a.MaxDate)
				);
			}
			return Json(
				comments
					.Where(c => c.ParentCommentId == null)
					.OrderByDescending(
						comment => comment.IsAuthor || comment.Children.Any(
							child => child.IsAuthor
						)
					)
					.ThenByDescending(c => c.MaxDate)
			);
		}
		[HttpPost]
		public async Task<IActionResult> CommentDeletion(
			[FromBody] CommentDeletionForm form,
			[FromServices] CommentingService commentingService,
			[FromServices] ObfuscationService obfuscationService
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var comment = await db.GetComment(obfuscationService.DecodeSingle(form.CommentId).Value);
				if (comment.UserAccountId == User.GetUserAccountId()) {
					return Json(
						new CommentThread(
							comment: await commentingService.DeleteComment(comment.Id),
							badge: LeaderboardBadge.None,
							isAuthor: false,
							obfuscationService: obfuscationService
						)
					);
				}
			}
			return BadRequest();
		}
		[HttpPost]
		public async Task<IActionResult> Follow(
			[FromServices] NotificationService notificationService,
			[FromBody] UserNameForm form
		) {
			Following following;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				following = await db.CreateFollowing(
					followerUserId: User.GetUserAccountId(),
					followeeUserName: form.UserName,
					analytics: this.GetClientAnalytics()
				);
			}
			await notificationService.CreateFollowerNotification(following);
			return Ok();
		}
		[HttpGet]
		public async Task<JsonResult> Followees() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					data: (await db
						.GetFollowees(
							userAccountId: User.GetUserAccountId()
						))
						.Select(
							followeeUserName => new Follower {
								UserName = followeeUserName,
								IsFollowed = true,
								HasAlert = false
							}
						)
				);
			}
		}
		[HttpGet]
		public async Task<JsonResult> FolloweesPosts(
			[FromServices] ObfuscationService obfuscationService,
			[FromQuery] FolloweesPostsQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountId();
				var postPageResult = await PageResult<api.DataAccess.Models.Post>.CreateAsync(
					await db.GetPostsFromFollowees(
						userId: userAccountId,
						pageNumber: query.PageNumber,
						pageSize: 40,
						minLength: query.MinLength,
						maxLength: query.MaxLength
					),
					async references => await db.GetPostsAsync(
						postReferences: references.ToArray(),
						userAccountId: userAccountId,
						alertEventTypes: new [] {
							NotificationEventType.Post
						}
					)
				);
				var articles = await db.GetArticlesAsync(
					postPageResult.Items
						.Select(
							post => post.ArticleId
						)
						.Distinct()
						.ToArray(),
					userAccountId: userAccountId
				);
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							post => new Post(
								article: articles.Single(
									article => article.Id == post.ArticleId
								),
								date: post.DateCreated,
								userName: post.UserName,
								badge: leaderboards.GetBadge(post.UserName),
								comment: (
									post.CommentId.HasValue ?
										new PostComment(
											post: post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									post.SilentPostId.HasValue ?
										obfuscationService.Encode(post.SilentPostId.Value) :
										null
								),
								dateDeleted: post.DateDeleted,
								hasAlert: post.HasAlert
							)
						)
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<JsonResult> Followers(
			[FromQuery] UserNameQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					data: await db.GetFollowers(
						viewerUserId: User.GetUserAccountIdOrDefault(),
						subjectUserName: query.UserName
					)
				);
			}
		}
		[HttpGet]
		public async Task<JsonResult> InboxPosts(
			[FromServices] ObfuscationService obfuscationService,
			[FromQuery] InboxPostsQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountId();
				var postPageResult = await PageResult<api.DataAccess.Models.Post>.CreateAsync(
					await db.GetPostsFromInbox(
						userId: userAccountId,
						pageNumber: query.PageNumber,
						pageSize: 40
					),
					async references => await db.GetPostsAsync(
						postReferences: references.ToArray(),
						userAccountId: userAccountId,
						alertEventTypes: new [] {
							NotificationEventType.Reply,
							NotificationEventType.Loopback
						}
					)
				);
				var articles = await db.GetArticlesAsync(
					postPageResult.Items
						.Select(
							post => post.ArticleId
						)
						.Distinct()
						.ToArray(),
					userAccountId: userAccountId
				);
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							post => new Post(
								article: articles.Single(
									article => article.Id == post.ArticleId
								),
								date: post.DateCreated,
								userName: post.UserName,
								badge: leaderboards.GetBadge(post.UserName),
								comment: (
									post.CommentId.HasValue ?
										new PostComment(
											post: post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									post.SilentPostId.HasValue ?
										obfuscationService.Encode(post.SilentPostId.Value) :
										null
								),
								dateDeleted: post.DateDeleted,
								hasAlert: post.HasAlert
							)
						)
					)
				);
			}
		}
		[HttpGet]
		public async Task<JsonResult> NotificationPosts(
			[FromServices] ObfuscationService obfuscationService,
			[FromQuery] NotificationPostsQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountId();
				var postPageResult = await PageResult<api.DataAccess.Models.Post>.CreateAsync(
					await db.GetNotificationPosts(
						userId: userAccountId,
						pageNumber: query.PageNumber,
						pageSize: 40
					),
					async references => await db.GetPostsAsync(
						postReferences: references.ToArray(),
						userAccountId: userAccountId,
						alertEventTypes: new [] {
							NotificationEventType.Post,
							NotificationEventType.Loopback
						}
					)
				);
				var articles = await db.GetArticlesAsync(
					postPageResult.Items
						.Select(
							post => post.ArticleId
						)
						.Distinct()
						.ToArray(),
					userAccountId: userAccountId
				);
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							post => new Post(
								article: articles.Single(
									article => article.Id == post.ArticleId
								),
								date: post.DateCreated,
								userName: post.UserName,
								badge: leaderboards.GetBadge(post.UserName),
								comment: (
									post.CommentId.HasValue ?
										new PostComment(
											post: post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									post.SilentPostId.HasValue ?
										obfuscationService.Encode(post.SilentPostId.Value) :
										null
								),
								dateDeleted: post.DateDeleted,
								hasAlert: post.HasAlert
							)
						)
					)
				);
			}
		}
		[HttpPost]
		public async Task<IActionResult> Post(
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] NotificationService notificationService,
			[FromServices] CommentingService commentingService,
			[FromBody] PostForm form
		) {
			var userAccountId = User.GetUserAccountId();
			var analytics = this.GetClientAnalytics();
			Article article;
			Comment comment;
			SilentPost silentPost;
			UserAccount user;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				article = await db.GetArticleById(
					articleId: form.ArticleId,
					userAccountId: userAccountId
				);
				if (!article.IsRead) {
					return BadRequest();
				}
				user = await db.GetUserAccountById(userAccountId);
			}
			try {
				if (commentingService.IsCommentTextValid(form.CommentText)) {
					comment = await commentingService.PostComment(
						text: form.CommentText,
						articleId: form.ArticleId,
						user: user,
						tweet: form.Tweet,
						analytics: analytics
					);
					silentPost = null;
				} else {
					silentPost = await commentingService.PostSilentPost(
						article: article,
						user: user,
						tweet: form.Tweet,
						analytics: analytics
					);
					comment = null;
				}
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to create new post.");
				return BadRequest();
			}
			LeaderboardBadge badge;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				if (form.RatingScore.HasValue) {
					await db.RateArticle(
						articleId: form.ArticleId,
						userAccountId: userAccountId,
						score: form.RatingScore.Value
					);
				}
				article = await db.GetArticleById(
					articleId: form.ArticleId,
					userAccountId: userAccountId
				);
				badge = (
						await db.GetUserLeaderboardRankings(
							userAccountId: userAccountId
						)
					)
					.GetBadge();
			}
			return Json(
				data: (
					comment != null ?
						new Post(
							article: article,
							date: comment.DateCreated,
							userName: comment.UserAccount,
							badge: badge,
							comment: new PostComment(
								comment: comment,
								obfuscationService: obfuscationService
							),
							silentPostId: null,
							dateDeleted: comment.DateDeleted,
							hasAlert: false
						) :
						new Post(
							article: article,
							date: silentPost.DateCreated,
							userName: user.Name,
							badge: badge,
							comment: null,
							silentPostId: obfuscationService.Encode(silentPost.Id),
							dateDeleted: null,
							hasAlert: false
						)
				)
			);
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<JsonResult> Profile(
			[FromQuery] UserNameQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var profile = await db.GetProfile(
					viewerUserId: User.GetUserAccountIdOrDefault(),
					subjectUserName: query.UserName
				);
				AuthorProfileClientModel authorProfile;
				var linkedAuthor = await db.GetAuthorByUserAccountName(
					userAccountName: query.UserName
				);
				if (linkedAuthor != null) {
					var distributionReport = await db.RunAuthorDistributionReportForSubscriptionPeriodDistributionsAsync(
						authorId: linkedAuthor.Id
					);
					authorProfile = new AuthorProfileClientModel(
						name: linkedAuthor.Name,
						slug: linkedAuthor.Slug,
						totalEarnings: distributionReport.Amount,
						totalPayouts: 0,
						userName: profile.UserName,
						donationRecipient: null
					);
				} else {
					authorProfile = null;
				}
				return Json(
					data: new ProfileClientModel(
						profile: profile,
						leaderboardBadge: (
								await db.GetUserLeaderboardRankings(
									userAccountId: await db.GetUserAccountIdByName(
										userName: query.UserName
									)
								)
							)
							.GetBadge(),
						authorProfile: authorProfile
					)
				);
			}
		}
		[HttpGet]
		public async Task<JsonResult> ReplyPosts(
			[FromServices] ObfuscationService obfuscationService,
			[FromQuery] ReplyPostsQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountId();
				var postPageResult = await PageResult<api.DataAccess.Models.Post>.CreateAsync(
					await db.GetReplyPosts(
						userId: userAccountId,
						pageNumber: query.PageNumber,
						pageSize: 40
					),
					async references => await db.GetPostsAsync(
						postReferences: references.ToArray(),
						userAccountId: userAccountId,
						alertEventTypes: new [] {
							NotificationEventType.Reply
						}
					)
				);
				var articles = await db.GetArticlesAsync(
					postPageResult.Items
						.Select(
							post => post.ArticleId
						)
						.Distinct()
						.ToArray(),
					userAccountId: userAccountId
				);
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							post => new Post(
								article: articles.Single(
									article => article.Id == post.ArticleId
								),
								date: post.DateCreated,
								userName: post.UserName,
								badge: leaderboards.GetBadge(post.UserName),
								comment: (
									post.CommentId.HasValue ?
										new PostComment(
											post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									post.SilentPostId.HasValue ?
										obfuscationService.Encode(post.SilentPostId.Value) :
										null
								),
								dateDeleted: post.DateDeleted,
								hasAlert: post.HasAlert
							)
						)
					)
				);
			}
		}
		[HttpPost]
		public async Task<IActionResult> Unfollow(
			[FromBody] UserNameForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await db.Unfollow(
					followerUserId: User.GetUserAccountId(),
					followeeUserName: form.UserName,
					analytics: this.GetClientAnalytics()
				);
				return Ok();
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<JsonResult> UserPosts(
			[FromServices] ObfuscationService obfuscationService,
			[FromQuery] UserPostsQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var postPageResult = await PageResult<api.DataAccess.Models.Post>.CreateAsync(
					await db.GetPostsFromUser(
						subjectUserName: query.UserName,
						pageNumber: query.PageNumber,
						pageSize: query.PageSize
					),
					async references => await db.GetPostsAsync(
						postReferences: references.ToArray(),
						userAccountId: User.GetUserAccountIdOrDefault(),
						alertEventTypes: new NotificationEventType[0]
					)
				);
				var articles = await db.GetArticlesAsync(
					postPageResult.Items
						.Select(
							post => post.ArticleId
						)
						.Distinct()
						.ToArray(),
					userAccountId: User.GetUserAccountIdOrDefault()
				);
				var selectedUserBadge = (
						await db.GetUserLeaderboardRankings(
							userAccountId: await db.GetUserAccountIdByName(
								userName: query.UserName
							)
						)
					)
					.GetBadge();
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							post => new Post(
								article: articles.Single(
									article => article.Id == post.ArticleId
								),
								date: post.DateCreated,
								userName: post.UserName,
								hasAlert: post.HasAlert,
								badge: selectedUserBadge,
								comment: (
									post.CommentId.HasValue ?
										new PostComment(
											post: post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									post.SilentPostId.HasValue ?
										obfuscationService.Encode(post.SilentPostId.Value) :
										null
								),
								dateDeleted: post.DateDeleted
							)
						)
					)
				);
			}
		}
	}
}