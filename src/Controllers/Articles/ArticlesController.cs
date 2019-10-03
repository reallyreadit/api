using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using api.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Net;
using Microsoft.Extensions.Options;
using api.Configuration;
using api.Messaging;
using System.Threading.Tasks;
using api.DataAccess.Models;
using Npgsql;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using api.ReadingVerification;
using api.Encryption;
using api.ClientModels;
using api.Analytics;
using api.DataAccess.Stats;
using api.Notifications;
using api.Commenting;

namespace api.Controllers.Articles {
	public class ArticlesController : Controller {
		private DatabaseOptions dbOpts;
		private readonly ILogger<ArticlesController> log;
		public ArticlesController(IOptions<DatabaseOptions> dbOpts, ILogger<ArticlesController> log) {
			this.dbOpts = dbOpts.Value;
			this.log = log;
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SetAotd(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromServices] NotificationService notifications,
			[FromForm] AotdForm form
		) {
			if (form.ApiKey == authOptions.Value.ApiKey) {
				using (
					var db = new NpgsqlConnection(
						connectionString: dbOpts.ConnectionString
					)
				) {
					var article = await db.SetAotd();
					await notifications.CreateAotdNotifications(
						articleId: article.Id
					);
					return Ok();
				}
			}
			return BadRequest();
		}
		// Deprecated 2018-12-18
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> ListHotTopics(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int pageSize
		) {
			return await CommunityReads(verificationService, pageNumber, pageSize, CommunityReadSort.Hot);
		}
		[HttpGet]
		public async Task<IActionResult> CommunityReads(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int pageSize,
			CommunityReadSort sort,
			CommunityReadTimeWindow? timeWindow = null,
			int? minLength = null,
			int? maxLength = null
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				PageResult<Article> articles;
				if (sort == CommunityReadSort.Hot || sort == CommunityReadSort.Top) {
					switch (sort) {
						case CommunityReadSort.Hot:
							articles = await db.GetHotArticles(
								userAccountId: userAccountId,
								pageNumber: pageNumber,
								pageSize: pageSize,
								minLength: minLength,
								maxLength: maxLength
							);
							break;
						case CommunityReadSort.Top:
							articles = await db.GetTopArticles(
								userAccountId: userAccountId,
								pageNumber: pageNumber,
								pageSize: pageSize,
								minLength: minLength,
								maxLength: maxLength
							);
							break;
						default:
							throw new ArgumentException($"Unexpected value for {nameof(sort)}");
					}
				} else {
					DateTime? sinceDate;
					if (timeWindow == CommunityReadTimeWindow.AllTime) {
						sinceDate = null;
					} else {
						var now = DateTime.UtcNow;
						switch (timeWindow) {
							case CommunityReadTimeWindow.PastDay:
								sinceDate = now.AddDays(-1);
								break;
							case CommunityReadTimeWindow.PastWeek:
								sinceDate = now.AddDays(-7);
								break;
							case CommunityReadTimeWindow.PastMonth:
								sinceDate = now.AddMonths(-1);
								break;
							case CommunityReadTimeWindow.PastYear:
								sinceDate = now.AddYears(-1);
								break;
							default:
								throw new ArgumentException($"Unexpected value for {nameof(timeWindow)}");
						}
					}
					switch (sort) {
						case CommunityReadSort.MostComments:
							articles = await db.GetMostCommentedArticles(
								userAccountId: userAccountId,
								pageNumber: pageNumber,
								pageSize: pageSize,
								sinceDate: sinceDate,
								minLength: minLength,
								maxLength: maxLength
							);
							break;
						case CommunityReadSort.MostRead:
							articles = await db.GetMostReadArticles(
								userAccountId: userAccountId,
								pageNumber: pageNumber,
								pageSize: pageSize,
								sinceDate: sinceDate,
								minLength: minLength,
								maxLength: maxLength
							);
							break;
						case CommunityReadSort.HighestRated:
							articles = await db.GetHighestRatedArticles(
								userAccountId: userAccountId,
								pageNumber: pageNumber,
								pageSize: pageSize,
								sinceDate: sinceDate,
								minLength: minLength,
								maxLength: maxLength
							);
							break;
						default:
							throw new ArgumentException($"Unexpected value for {nameof(sort)}");
					}
				}
				var aotd = verificationService.AssignProofToken(await db.GetAotd(userAccountId), userAccountId);
				var articlePageResult = PageResult<Article>.Create(
					articles,
					items => items.Select(article => verificationService.AssignProofToken(article, userAccountId))
				);
				var userReadCount = await db.GetUserReadCount(userAccountId: userAccountId);
				if (this.ClientVersionIsGreaterThanOrEqualTo(
					new Dictionary<ClientType, SemanticVersion>() {
						{ ClientType.WebAppServer, new SemanticVersion("1.4.0") },
						{ ClientType.WebAppClient, new SemanticVersion("1.4.0") }
					}
				)) {
					return Json(
						new {
							Aotd = aotd,
							AotdHasAlert = (
								(
									await db.GetUserAccountById(
										userAccountId: userAccountId
									)
								)
								.AotdAlert
							),
							Articles = articlePageResult,
							UserReadCount = userReadCount
						}
					);
				} else {
					return Json(
						new {
							Aotd = aotd,
							Articles = articlePageResult,
							UserStats = new {
								ReadCount = userReadCount
							}
						}
					);
				}
			}
		}
		[HttpGet]
		public IActionResult ListStarred(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int? minLength = null,
			int? maxLength = null
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					PageResult<Article>.Create(
						db.GetStarredArticles(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: 40,
							minLength: minLength,
							maxLength: maxLength
						),
						articles => articles.Select(article => verificationService.AssignProofToken(article, userAccountId))
					)
				);
			}
		}
		[HttpGet]
		public async Task<IActionResult> ListHistory(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int? minLength = null,
			int? maxLength = null
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					PageResult<Article>.Create(
						await db.GetArticleHistory(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: 40,
							minLength: minLength,
							maxLength: maxLength
						),
						articles => articles.Select(article => verificationService.AssignProofToken(article, userAccountId))
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult Details(
			[FromServices] ReadingVerificationService verificationService,
			string slug
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountIdOrDefault();
				var article = db.FindArticle(slug, userAccountId);
				return Json(
					userAccountId.HasValue ?
						verificationService.AssignProofToken(
							article: article,
							userAccountId: userAccountId.Value
						) :
						article
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> ListComments(
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] ReadingVerificationService verificationService,
			string slug
		) {
			var userAccountId = User.GetUserAccountIdOrDefault();
			CommentThread[] comments;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId ?? 0,
					now: DateTime.UtcNow
				);
				comments = db
					.ListComments(
						db.FindArticle(slug, userAccountId).Id
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
		public async Task<IActionResult> PostComment(
			[FromBody] PostCommentBinder binder,
			[FromServices] CommentingService commentingService,
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] ReadingVerificationService verificationService
		) {
			if (!String.IsNullOrWhiteSpace(binder.Text)) {
				var userAccountId = this.User.GetUserAccountId();
				using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
					var userArticle = await db.GetArticle(binder.ArticleId, userAccountId);
					if (userArticle.IsRead && commentingService.IsCommentTextValid(binder.Text)) {
						var commentThread = new CommentThread(
							comment: await commentingService.PostComment(
								dbConnection: db,
								text: binder.Text,
								articleId: binder.ArticleId,
								parentCommentId: obfuscationService.Decode(binder.ParentCommentId),
								userAccountId: userAccountId,
								analytics: this.GetRequestAnalytics()
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
									article: await db.GetArticle(binder.ArticleId, userAccountId),
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
		[HttpGet]
		public async Task<IActionResult> ListReplies(int pageNumber, [FromServices] ObfuscationService obfuscationService) {
			var userAccountId = this.User.GetUserAccountId();
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var leaderboards = await db.GetLeaderboards(
					userAccountId: userAccountId,
					now: DateTime.UtcNow
				);
				return Json(PageResult<CommentThread>.Create(
					source: db.ListReplies(this.User.GetUserAccountId(), pageNumber, 40),
					map: comments => comments.Select(c => new CommentThread(
						comment: c,
						badge: leaderboards.GetBadge(
							userName: c.UserAccount
						),
						obfuscationService: obfuscationService
					))
				));
			}
		}
		[HttpPost]
		public async Task<IActionResult> ReadReply(
			[FromServices] ObfuscationService obfuscationService,
			[FromBody] ReadReplyBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var comment = await db.GetComment(obfuscationService.Decode(binder.CommentId).Value);
				if ((await db.GetComment(comment.ParentCommentId.Value)).UserAccountId == User.GetUserAccountId()) {
					db.ReadComment(comment.Id);
					return Ok();
				}
			}
			return BadRequest();
		}
		[HttpPost]
		public async Task<IActionResult> Star(
			[FromBody] ArticleIdBinder binder,
			[FromServices] ReadingVerificationService verificationService
		) {
			var userAccountId = this.User.GetUserAccountId();
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.StarArticle(userAccountId, binder.ArticleId);
				return Json(verificationService.AssignProofToken(
					article: await db.GetArticle(binder.ArticleId, userAccountId),
					userAccountId: userAccountId
				));
			}
		}
		[HttpPost]
		public async Task<IActionResult> Unstar(
			[FromBody] ArticleIdBinder binder,
			[FromServices] ReadingVerificationService verificationService
		) {
			var userAccountId = this.User.GetUserAccountId();
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.UnstarArticle(userAccountId, binder.ArticleId);
				return Json(verificationService.AssignProofToken(
					article: await db.GetArticle(binder.ArticleId, userAccountId),
					userAccountId: userAccountId
				));
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> VerifyProofToken(
			[FromServices] ReadingVerificationService verificationService,
			string token
		) {
			var tokenData = verificationService.GetTokenData(token);
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var readerName = (await db.GetUserAccountById(tokenData.UserAccountId)).Name;
				Article article;
				if (this.User.Identity.IsAuthenticated) {
					var userAccountId = this.User.GetUserAccountId();
					article = verificationService.AssignProofToken(
						await db.GetArticle(tokenData.ArticleId, userAccountId),
						userAccountId
					);
				} else {
					article = await db.GetArticle(tokenData.ArticleId, null);
				}
				return Json(new {
					Article = article,
					ReaderName = readerName
				});
			}
		}
		[HttpPost]
		public async Task<IActionResult> Rate(
			[FromBody] ArticleRatingForm form,
			[FromServices] ReadingVerificationService verificationService
		) {
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var rating = await db.RateArticle(
					articleId: form.ArticleId,
					userAccountId: userAccountId,
					score: form.Score
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
						Rating = rating
					});
				} else {
					return Json(rating);
				}
			}
		}
	}
}