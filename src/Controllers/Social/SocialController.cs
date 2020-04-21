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
						var commentThread = new CommentThread(
							comment: await commentingService.PostReply(
								text: form.Text,
								articleId: form.ArticleId,
								parentCommentId: obfuscationService.Decode(form.ParentCommentId).Value,
								userAccountId: userAccountId,
								analytics: this.GetClientAnalytics()
							),
							badge: (
									await db.GetUserLeaderboardRankings(
										userAccountId: userAccountId
									)
								)
								.GetBadge(),
							obfuscationService: obfuscationService
						);
						if (
							this.ClientVersionIsGreaterThanOrEqualTo(new Dictionary<ClientType, SemanticVersion>() {
								{ ClientType.WebAppClient, new SemanticVersion("1.0.0") },
								{ ClientType.WebExtension, new SemanticVersion("1.0.0") },
								{ ClientType.IosApp, new SemanticVersion("3.1.1") }
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
				var comment = await db.GetComment(obfuscationService.Decode(form.CommentId).Value);
				if (comment.UserAccountId == User.GetUserAccountId()) {
					return Json(
						new CommentThread(
							await commentingService.CreateAddendum(comment.Id, form.Text),
							(await db.GetUserLeaderboardRankings(User.GetUserAccountId())).GetBadge(),
							obfuscationService
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
				var comment = await db.GetComment(obfuscationService.Decode(form.CommentId).Value);
				if (
					comment.UserAccountId == User.GetUserAccountId() &&
					commentingService.CanReviseComment(comment)
				) {
					return Json(
						new CommentThread(
							await commentingService.ReviseComment(comment.Id, form.Text),
							(await db.GetUserLeaderboardRankings(User.GetUserAccountId())).GetBadge(),
							obfuscationService
						)
					);
				}
			}
			return BadRequest();
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Comments(
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] ReadingVerificationService verificationService,
			[FromQuery] CommentsQuery query
		) {
			var userAccountId = User.GetUserAccountIdOrDefault();
			CommentThread[] comments;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId ?? 0,
					now: DateTime.UtcNow
				);
				comments = (
						await db.GetComments(
							db.FindArticle(query.Slug, userAccountId).Id
						)
					)
					.Select(
						comment => new CommentThread(
							comment: comment,
							badge: leaderboards.GetBadge(comment.UserAccount),
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
				var comment = await db.GetComment(obfuscationService.Decode(form.CommentId).Value);
				if (comment.UserAccountId == User.GetUserAccountId()) {
					return Json(
						new CommentThread(
							await commentingService.DeleteComment(comment.Id),
							LeaderboardBadge.None,
							obfuscationService
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
			string userName;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var article = await db.GetArticle(
					articleId: form.ArticleId,
					userAccountId: userAccountId
				);
				if (article.IsRead) {
					try {
						if (commentingService.IsCommentTextValid(form.CommentText)) {
							comment = await commentingService.PostComment(
								text: form.CommentText,
								articleId: form.ArticleId,
								userAccountId: userAccountId,
								tweet: form.Tweet,
								analytics: analytics
							);
							silentPost = null;
							userName = comment.UserAccount;
						} else {
							silentPost = await commentingService.PostSilentPost(
								articleId: form.ArticleId,
								articleSlug: article.Slug,
								userAccountId: userAccountId,
								tweet: form.Tweet,
								analytics: analytics
							);
							comment = null;
							userName = (await db.GetUserAccountById(userAccountId)).Name;
							await notificationService.CreatePostNotifications(
								userAccountId: userAccountId,
								userName: userName,
								articleId: article.Id,
								articleTitle: article.Title,
								commentId: null,
								commentText: null,
								silentPostId: silentPost.Id
							);
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
									userName: userName,
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
				return Json(
					data: new Profile(
						dbProfile: await db.GetProfile(
							viewerUserId: User.GetUserAccountIdOrDefault(),
							subjectUserName: query.UserName
						),
						leaderboardBadge: (
								await db.GetUserLeaderboardRankings(
									userAccountId: await db.GetUserAccountIdByName(
										userName: query.UserName
									)
								)
							)
							.GetBadge()
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