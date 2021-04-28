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
			if (!String.IsNullOrWhiteSpace(form.Text)) {
				var userAccountId = this.User.GetUserAccountId();
				using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
					var userArticle = await db.GetArticle(form.ArticleId, userAccountId);
					if (userArticle.IsRead && commentingService.IsCommentTextValid(form.Text)) {
						var articleAuthors = await db.GetAuthorsOfArticle(
							articleId: form.ArticleId
						);
						var commentThread = new CommentThread(
							comment: await commentingService.PostReply(
								text: form.Text,
								articleId: form.ArticleId,
								parentCommentId: obfuscationService.DecodeSingle(form.ParentCommentId).Value,
								userAccountId: userAccountId,
								analytics: this.GetClientAnalytics()
							),
							badge: (
									await db.GetUserLeaderboardRankings(
										userAccountId: userAccountId
									)
								)
								.GetBadge(),
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
									article: await db.GetArticle(form.ArticleId, userAccountId),
									userAccountId: userAccountId
								),
								Comment = commentThread
							});
						}
						return Json(commentThread);
					}
				}
			}
			return BadRequest();
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
				var article = await db.FindArticle(query.Slug, userAccountId);
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
				comment.Children.Sort((a, b) => b.MaxDate.CompareTo(a.MaxDate));
			}
			return Json(
				comments
					.Where(c => c.ParentCommentId == null)
					.OrderByDescending(c => c.MaxDate)
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
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await notificationService.CreateFollowerNotification(
					following: await db.CreateFollowing(
						followerUserId: User.GetUserAccountId(),
						followeeUserName: form.UserName,
						analytics: this.GetClientAnalytics()
					)
				);
				return Ok();
			}
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
				var postPageResult = await db.GetPostsFromFollowees(
					userId: userAccountId,
					pageNumber: query.PageNumber,
					pageSize: 40,
					minLength: query.MinLength,
					maxLength: query.MaxLength
				);
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							multimap => new Post(
								article: multimap.Article,
								date: multimap.Post.PostDateCreated,
								userName: multimap.Post.UserName,
								badge: leaderboards.GetBadge(multimap.Post.UserName),
								comment: (
									multimap.Post.CommentId.HasValue ?
										new PostComment(
											post: multimap.Post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									multimap.Post.SilentPostId.HasValue ?
										obfuscationService.Encode(multimap.Post.SilentPostId.Value) :
										null
								),
								dateDeleted: multimap.Post.DateDeleted,
								hasAlert: multimap.Post.HasAlert
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
				var postPageResult = await db.GetPostsFromInbox(
					userId: userAccountId,
					pageNumber: query.PageNumber,
					pageSize: 40
				);
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							multimap => new Post(
								article: multimap.Article,
								date: multimap.Post.PostDateCreated,
								userName: multimap.Post.UserName,
								badge: leaderboards.GetBadge(multimap.Post.UserName),
								comment: (
									multimap.Post.CommentId.HasValue ?
										new PostComment(
											post: multimap.Post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									multimap.Post.SilentPostId.HasValue ?
										obfuscationService.Encode(multimap.Post.SilentPostId.Value) :
										null
								),
								dateDeleted: multimap.Post.DateDeleted,
								hasAlert: multimap.Post.HasAlert
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
				var postPageResult = await db.GetNotificationPosts(
					userId: userAccountId,
					pageNumber: query.PageNumber,
					pageSize: 40
				);
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							multimap => new Post(
								article: multimap.Article,
								date: multimap.Post.PostDateCreated,
								userName: multimap.Post.UserName,
								badge: leaderboards.GetBadge(multimap.Post.UserName),
								comment: (
									multimap.Post.CommentId.HasValue ?
										new PostComment(
											post: multimap.Post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									multimap.Post.SilentPostId.HasValue ?
										obfuscationService.Encode(multimap.Post.SilentPostId.Value) :
										null
								),
								dateDeleted: multimap.Post.DateDeleted,
								hasAlert: multimap.Post.HasAlert
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
			Comment comment;
			SilentPost silentPost;
			UserAccount user;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var article = await db.GetArticle(
					articleId: form.ArticleId,
					userAccountId: userAccountId
				);
				if (article.IsRead) {
					try {
						user = await db.GetUserAccountById(userAccountId);
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
					if (form.RatingScore.HasValue) {
						await db.RateArticle(
							articleId: form.ArticleId,
							userAccountId: userAccountId,
							score: form.RatingScore.Value
						);
					}
					article = await db.GetArticle(
						articleId: form.ArticleId,
						userAccountId: userAccountId
					);
					var badge = (
							await db.GetUserLeaderboardRankings(
								userAccountId: userAccountId
							)
						)
						.GetBadge();
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
				return BadRequest();
			}
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
						userName: profile.UserName
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
				var postPageResult = await db.GetReplyPosts(
					userId: userAccountId,
					pageNumber: query.PageNumber,
					pageSize: 40
				);
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(
					data: PageResult<Post>.Create(
						source: postPageResult,
						map: results => results.Select(
							multimap => new Post(
								article: multimap.Article,
								date: multimap.Post.PostDateCreated,
								userName: multimap.Post.UserName,
								badge: leaderboards.GetBadge(multimap.Post.UserName),
								comment: (
									multimap.Post.CommentId.HasValue ?
										new PostComment(
											post: multimap.Post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									multimap.Post.SilentPostId.HasValue ?
										obfuscationService.Encode(multimap.Post.SilentPostId.Value) :
										null
								),
								dateDeleted: multimap.Post.DateDeleted,
								hasAlert: multimap.Post.HasAlert
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
				var postPageResult = await db.GetPostsFromUser(
					viewerUserId: User.GetUserAccountIdOrDefault(),
					subjectUserName: query.UserName,
					pageNumber: query.PageNumber,
					pageSize: query.PageSize
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
							multimap => new Post(
								article: multimap.Article,
								date: multimap.Post.PostDateCreated,
								userName: multimap.Post.UserName,
								hasAlert: multimap.Post.HasAlert,
								badge: selectedUserBadge,
								comment: (
									multimap.Post.CommentId.HasValue ?
										new PostComment(
											post: multimap.Post,
											obfuscationService: obfuscationService
										) :
										null
								),
								silentPostId: (
									multimap.Post.SilentPostId.HasValue ?
										obfuscationService.Encode(multimap.Post.SilentPostId.Value) :
										null
								),
								dateDeleted: multimap.Post.DateDeleted
							)
						)
					)
				);
			}
		}
	}
}