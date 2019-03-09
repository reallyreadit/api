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
						Aotd = verificationService.AssignProofToken(await db.GetUserAotd(userAccountId), userAccountId),
						Articles = PageResult<UserArticle>.Create(
							await db.ListUserCommunityReads(userAccountId, pageNumber, pageSize, sort),
							articles => articles.Select(article => verificationService.AssignProofToken(article, userAccountId))
						),
						UserStats = await db.GetUserStats(userAccountId)
					});
				}
				return Json(new {
					Aotd = await db.GetAotd(),
					Articles = await db.ListCommunityReads(pageNumber, pageSize, sort)
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
					PageResult<UserArticle>.Create(
						db.ListStarredArticles(userAccountId, pageNumber, 40),
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
					PageResult<UserArticle>.Create(
						db.ListUserArticleHistory(userAccountId, pageNumber, 40),
						articles => articles.Select(article => verificationService.AssignProofToken(article, userAccountId))
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Details(
			[FromServices] ReadingVerificationService verificationService,
			string proofToken,
			string slug
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				if (this.User.Identity.IsAuthenticated) {
					var userAccountId = this.User.GetUserAccountId();
					return Json(verificationService.AssignProofToken(
						article: slug != null ?
							db.FindUserArticle(slug, userAccountId) :
							db.GetUserArticle(verificationService.GetTokenData(proofToken).ArticleId, userAccountId),
						userAccountId: userAccountId
					));
				} else {
					return Json(
						slug != null ?
							db.FindArticle(slug) :
							await db.GetArticle(verificationService.GetTokenData(proofToken).ArticleId)
					);
				}
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ListComments(
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] ReadingVerificationService verificationService,
			string proofToken,
			string slug
		) {
			CommentThread[] comments;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				comments = db
					.ListComments(
						slug != null ?
							db.FindArticle(slug).Id :
							verificationService.GetTokenData(proofToken).ArticleId
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
					var userArticle = db.GetUserArticle(binder.ArticleId, this.User.GetUserAccountId());
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
		[HttpPost]
		public IActionResult UserDelete([FromBody] ArticleIdBinder binder) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				var article = db.GetUserArticle(binder.ArticleId, userAccountId);
				if (article.DateStarred.HasValue) {
					db.UnstarArticle(userAccountId, article.Id);
				}
				if (article.DateCreated.HasValue) {
					db.DeleteUserArticle(article.Id, userAccountId);
				}
			}
			return Ok();
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
		[HttpPost]
		public async Task<IActionResult> Share(
			[FromBody] ShareArticleBinder binder,
			[FromServices] EmailService emailService,
			[FromServices] CaptchaService captchaService
		) {
			if (
				binder.EmailAddresses.Length < 1 ||
				binder.EmailAddresses.Length > 5 ||
				binder.EmailAddresses.Any(address => address.Length > 256) ||
				binder.Message?.Length > 10000
			) {
				return BadRequest();
			}
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var sender = db.GetUserAccount(User.GetUserAccountId());
				if (!sender.IsEmailConfirmed) {
					return BadRequest(new[] { "UnconfirmedEmail" });
				}
				var captchaResponse = await captchaService.Verify(binder.CaptchaResponse);
				if (captchaResponse != null) {
					db.CreateCaptchaResponse("shareArticle", captchaResponse);
				}
				var userArticle = db.GetUserArticle(binder.ArticleId, User.GetUserAccountId());
				if (userArticle.IsRead) {
					var recipients = new List<EmailShareRecipient>();
					foreach (var address in binder.EmailAddresses) {
						var recipient = db.FindUserAccount(address);
						bool sentSuccessfully;
						try {
							sentSuccessfully = await emailService.SendShareEmail(
								sender: sender,
								recipient: recipient != null ?
									recipient as IEmailRecipient :
									new EmailRecipient(address),
								article: userArticle,
								message: binder.Message
							);
						} catch (Exception ex) {
							log.LogError(500, ex, "Error sending share email.");
							sentSuccessfully = false;
						}
						recipients.Add(new EmailShareRecipient(address, recipient?.Id ?? 0, sentSuccessfully));
					}
					db.CreateEmailShare(
						dateSent: DateTime.UtcNow,
						articleId: userArticle.Id,
						userAccountId: sender.Id,
						message: binder.Message,
						recipientAddresses: recipients.Select(r => r.EmailAddress).ToArray(),
						recipientIds: recipients.Select(r => r.UserAccountId).ToArray(),
						recipientResults: recipients.Select(r => r.IsSuccessful).ToArray()
					);
					return Ok();
				}
				return BadRequest();
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
				var readerName = db.GetUserAccount(tokenData.UserAccountId).Name;
				UserArticle article;
				if (this.User.Identity.IsAuthenticated) {
					var userAccountId = this.User.GetUserAccountId();
					article = verificationService.AssignProofToken(
						db.GetUserArticle(tokenData.ArticleId, userAccountId),
						userAccountId
					);
				} else {
					article = await db.GetArticle(tokenData.ArticleId);
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