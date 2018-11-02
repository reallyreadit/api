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

namespace api.Controllers.Articles {
	public class ArticlesController : Controller {
		private DatabaseOptions dbOpts;
		private readonly ILogger<ArticlesController> log;
		public ArticlesController(IOptions<DatabaseOptions> dbOpts, ILogger<ArticlesController> log) {
			this.dbOpts = dbOpts.Value;
			this.log = log;
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> ListHotTopics(int pageNumber, int pageSize) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(this.User.Identity.IsAuthenticated ?
					new {
						Aotd = await db.GetUserAotd(this.User.GetUserAccountId()),
						Articles = await db.ListUserHotTopics(this.User.GetUserAccountId(), pageNumber, pageSize)
					} :
					new {
						Aotd = await db.GetAotd(),
						Articles = await db.ListHotTopics(pageNumber, pageSize)
					}
				);
			}
		}
		[HttpGet]
		public IActionResult ListStarred(int pageNumber) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.ListStarredArticles(this.User.GetUserAccountId(), pageNumber, 40));
			}
		}
		[HttpGet]
		public IActionResult ListHistory(int pageNumber) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.ListUserArticleHistory(this.User.GetUserAccountId(), pageNumber, 40));
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult Details(string slug) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(this.User.Identity.IsAuthenticated ?
					db.FindUserArticle(slug, this.User.GetUserAccountId()) :
					db.FindArticle(slug)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ListComments(string slug) {
			CommentThread[] comments;
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				comments = db
					.ListComments(db.FindArticle(slug).Id)
					.Select(c => new CommentThread(c))
					.ToArray();
			}
			foreach (var comment in comments.Where(c => c.ParentCommentId.HasValue)) {
				comments.Single(c => c.Id == comment.ParentCommentId).Children.Add(comment);
			}
			foreach (var comment in comments) {
				comment.Children.Sort((a, b) => b.MaxDate.CompareTo(a.MaxDate));
			}
			return Json(comments
				.Where(c => !c.ParentCommentId.HasValue)
				.OrderByDescending(c => c.MaxDate));
		}
		[HttpPost]
		public async Task<IActionResult> PostComment(
			[FromBody] PostCommentBinder binder,
			[FromServices] EmailService emailService
		) {
			if (!String.IsNullOrWhiteSpace(binder.Text)) {
				using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
					var userArticle = db.GetUserArticle(binder.ArticleId, this.User.GetUserAccountId());
					if (userArticle.IsRead) {
						var comment = db.CreateComment(WebUtility.HtmlEncode(binder.Text), binder.ArticleId, binder.ParentCommentId, this.User.GetUserAccountId());
						if (binder.ParentCommentId.HasValue) {
							var parent = db.GetComment(binder.ParentCommentId.Value);
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
						return Json(new CommentThread(comment));
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
		public IActionResult ListReplies(int pageNumber) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(PageResult<CommentThread>.Create(
					source: db.ListReplies(this.User.GetUserAccountId(), pageNumber, 40),
					map: comments => comments.Select(c => new CommentThread(c))
				));
			}
		}
		[HttpPost]
		public IActionResult ReadReply([FromBody] ReadReplyBinder binder) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var comment = db.GetComment(binder.CommentId);
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
				if (!await captchaService.IsValid("6LegNF4UAAAAAJvSX5a7aZXvgQ_4h0W2NzWiGmCe", binder.CaptchaResponse)) {
					return BadRequest(new[] { "InvalidCaptcha" });
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
	}
}