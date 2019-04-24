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
using api.Security;
using Microsoft.Extensions.Logging;
using api.ReadingVerification;
using api.Encryption;
using api.ClientModels;
using api.Versioning;

namespace api.Controllers.Articles {
	public class ArticlesController : Controller {
		private DatabaseOptions dbOpts;
		private readonly ILogger<ArticlesController> log;
		public ArticlesController(IOptions<DatabaseOptions> dbOpts, ILogger<ArticlesController> log) {
			this.dbOpts = dbOpts.Value;
			this.log = log;
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
			CommunityReadTimeWindow? timeWindow = null
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
								pageSize: pageSize
							);
							break;
						case CommunityReadSort.Top:
							articles = await db.GetTopArticles(
								userAccountId: userAccountId,
								pageNumber: pageNumber,
								pageSize: pageSize
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
								sinceDate: sinceDate
							);
							break;
						case CommunityReadSort.MostRead:
							articles = await db.GetMostReadArticles(
								userAccountId: userAccountId,
								pageNumber: pageNumber,
								pageSize: pageSize,
								sinceDate: sinceDate
							);
							break;
						case CommunityReadSort.HighestRated:
							articles = await db.GetHighestRatedArticles(
								userAccountId: userAccountId,
								pageNumber: pageNumber,
								pageSize: pageSize,
								sinceDate: sinceDate
							);
							break;
						default:
							throw new ArgumentException($"Unexpected value for {nameof(sort)}");
					}
				}
				return Json(new {
					Aotd = verificationService.AssignProofToken(await db.GetAotd(userAccountId), userAccountId),
					Articles = PageResult<Article>.Create(
						articles,
						items => items.Select(article => verificationService.AssignProofToken(article, userAccountId))
					),
					UserStats = await db.GetUserStats(userAccountId)
				});
			}
		}
		[HttpGet]
		public IActionResult ListStarred(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					PageResult<Article>.Create(
						db.GetStarredArticles(userAccountId, pageNumber, 40),
						articles => articles.Select(article => verificationService.AssignProofToken(article, userAccountId))
					)
				);
			}
		}
		[HttpGet]
		public IActionResult ListHistory(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					PageResult<Article>.Create(
						db.GetArticleHistory(userAccountId, pageNumber, 40),
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
		public IActionResult ListComments(
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] ReadingVerificationService verificationService,
			string slug
		) {
			CommentThread[] comments;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				comments = db
					.ListComments(
						db.FindArticle(slug, User.GetUserAccountIdOrDefault()).Id
					)
					.Select(c => new CommentThread(c, obfuscationService))
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
			[FromServices] EmailService emailService,
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] ReadingVerificationService verificationService
		) {
			if (!String.IsNullOrWhiteSpace(binder.Text)) {
				var userAccountId = this.User.GetUserAccountId();
				using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
					var userArticle = await db.GetArticle(binder.ArticleId, userAccountId);
					if (userArticle.IsRead) {
						var parentCommentId = obfuscationService.Decode(binder.ParentCommentId);
						var comment = db.CreateComment(WebUtility.HtmlEncode(binder.Text), binder.ArticleId, parentCommentId, userAccountId);
						if (parentCommentId != null) {
							var parent = db.GetComment(parentCommentId.Value);
							if (parent.UserAccountId != userAccountId) {
								var parentUserAccount = await db.GetUserAccount(parent.UserAccountId);
								if (parentUserAccount.ReceiveReplyEmailNotifications) {
									await emailService.SendCommentReplyNotificationEmail(
										recipient: parentUserAccount,
										reply: comment
									);
								}
							}
						}
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
								Comment = new CommentThread(comment, obfuscationService)
							});
						}
						return Json(new CommentThread(comment, obfuscationService));
					}
				}
			}
			return BadRequest();
		}
		[HttpGet]
		public IActionResult ListReplies(int pageNumber, [FromServices] ObfuscationService obfuscationService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(PageResult<CommentThread>.Create(
					source: db.ListReplies(this.User.GetUserAccountId(), pageNumber, 40),
					map: comments => comments.Select(c => new CommentThread(c, obfuscationService))
				));
			}
		}
		[HttpPost]
		public IActionResult ReadReply(
			[FromServices] ObfuscationService obfuscationService,
			[FromBody] ReadReplyBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var comment = db.GetComment(obfuscationService.Decode(binder.CommentId).Value);
				if (db.GetComment(comment.ParentCommentId.Value).UserAccountId == User.GetUserAccountId()) {
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
				var readerName = (await db.GetUserAccount(tokenData.UserAccountId)).Name;
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