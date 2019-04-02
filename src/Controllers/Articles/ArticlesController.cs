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
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> CommunityReads(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int pageSize,
			CommunityReadSort sort
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				if (this.User.Identity.IsAuthenticated) {
					var userAccountId = this.User.GetUserAccountId();
					return Json(new {
						Aotd = verificationService.AssignProofToken(await db.GetAotd(userAccountId), userAccountId),
						Articles = PageResult<Article>.Create(
							await db.GetCommunityReads(userAccountId, pageNumber, pageSize, sort),
							articles => articles.Select(article => verificationService.AssignProofToken(article, userAccountId))
						),
						UserStats = await db.GetUserStats(userAccountId)
					});
				}
				return Json(new {
					Aotd = await db.GetAotd(null),
					Articles = await db.GetCommunityReads(null, pageNumber, pageSize, sort)
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
				if (this.User.Identity.IsAuthenticated) {
					var userAccountId = this.User.GetUserAccountId();
					return Json(verificationService.AssignProofToken(
						article: db.FindArticle(slug, userAccountId),
						userAccountId: userAccountId
					));
				} else {
					return Json(db.FindArticle(slug, null));
				}
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
						db.FindArticle(slug, null).Id
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
			[FromServices] ObfuscationService obfuscationService
		) {
			if (!String.IsNullOrWhiteSpace(binder.Text)) {
				using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
					var userArticle = await db.GetArticle(binder.ArticleId, this.User.GetUserAccountId());
					if (userArticle.IsRead) {
						var parentCommentId = obfuscationService.Decode(binder.ParentCommentId);
						var comment = db.CreateComment(WebUtility.HtmlEncode(binder.Text), binder.ArticleId, parentCommentId, this.User.GetUserAccountId());
						if (parentCommentId != null) {
							var parent = db.GetComment(parentCommentId.Value);
							if (parent.UserAccountId != this.User.GetUserAccountId()) {
								var parentUserAccount = db.GetUserAccount(parent.UserAccountId);
								if (parentUserAccount.ReceiveReplyEmailNotifications) {
									await emailService.SendCommentReplyNotificationEmail(
										recipient: parentUserAccount,
										reply: comment
									);
								}
							}
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
		public IActionResult Star([FromBody] ArticleIdBinder binder) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.StarArticle(this.User.GetUserAccountId(), binder.ArticleId);
			}
			return Ok();
		}
		[HttpPost]
		public IActionResult Unstar([FromBody] ArticleIdBinder binder) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.UnstarArticle(this.User.GetUserAccountId(), binder.ArticleId);
			}
			return Ok();
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> VerifyProofToken(
			[FromServices] ReadingVerificationService verificationService,
			string token
		) {
			var tokenData = verificationService.GetTokenData(token);
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var readerName = db.GetUserAccount(tokenData.UserAccountId).Name;
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
		public async Task<IActionResult> Rate([FromBody] ArticleRatingForm form) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.RateArticle(
					articleId: form.ArticleId,
					userAccountId: User.GetUserAccountId(),
					score: form.Score
				));
			}
		}
	}
}