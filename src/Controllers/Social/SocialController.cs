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
using System.Net;
using api.ClientModels;
using api.DataAccess.Stats;
using api.Encryption;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace api.Controllers.Social {
	public class SocialController : Controller {
		private DatabaseOptions dbOpts;
		public SocialController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		[HttpPost]
		public async Task<IActionResult> Follow(
			[FromBody] UserNameForm form
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await db.CreateFollowing(
					followerUserId: User.GetUserAccountId(),
					followeeUserName: form.UserName,
					analytics: this.GetRequestAnalytics()
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
							followeeUserName => new Following {
								UserName = followeeUserName,
								IsFollowed = true
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
								)
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
		[HttpPost]
		public async Task<IActionResult> Post(
			[FromServices] ObfuscationService obfuscationService,
			[FromBody] PostForm form
		) {
			var userAccountId = User.GetUserAccountId();
			var analytics = this.GetRequestAnalytics();
			Comment comment;
			SilentPost silentPost;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var article = await db.GetArticle(
					articleId: form.ArticleId,
					userAccountId: userAccountId
				);
				if (article.IsRead) {
					try {
						if (!String.IsNullOrWhiteSpace(form.CommentText)) {
							comment = db.CreateComment(
								text: WebUtility.HtmlEncode(form.CommentText),
								articleId: form.ArticleId,
								parentCommentId: null,
								userAccountId: userAccountId,
								analytics: analytics
							);
							silentPost = null;
						} else {
							silentPost = await db.CreateSilentPost(
								userAccountId: userAccountId,
								articleId: form.ArticleId,
								analytics: analytics
							);
							comment = null;
						}
					} catch {
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
									)
								) :
								new Post(
									article: article,
									date: silentPost.DateCreated,
									userName: (await db.GetUserAccountById(userAccountId)).Name,
									badge: badge
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
					analytics: this.GetRequestAnalytics()
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
								badge: selectedUserBadge,
								comment: (
									multimap.Post.CommentId.HasValue ?
										new PostComment(
											post: multimap.Post,
											obfuscationService: obfuscationService
										) :
										null
								)
							)
						)
					)
				);
			}
		}
	}
}