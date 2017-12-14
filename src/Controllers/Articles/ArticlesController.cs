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

namespace api.Controllers.Articles {
	public class ArticlesController : Controller {
		private DbConnection db;
		public ArticlesController(DbConnection db) {
			this.db = db;
		}
		protected override void Dispose(bool disposing) {
			db.Dispose();
			base.Dispose(disposing);
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> ListHotTopics(int pageNumber) => Json(this.User.Identity.IsAuthenticated ?
			new {
				Aotd = await db.GetUserAotd(this.User.GetUserAccountId()),
				Articles = await db.ListUserHotTopics(this.User.GetUserAccountId(), pageNumber, 40)
			 } :
			new {
				Aotd = await db.GetAotd(),
				Articles = await db.ListHotTopics(pageNumber, 40)
			}
		);
		[HttpGet]
		public IActionResult ListStarred(int pageNumber) => Json(db.ListStarredArticles(this.User.GetUserAccountId(), pageNumber, 40));
		[HttpGet]
		public IActionResult ListHistory(int pageNumber) => Json(db.ListUserArticleHistory(this.User.GetUserAccountId(), pageNumber, 40));
		[AllowAnonymous]
		[HttpGet]
		public IActionResult Details(string slug) => Json(this.User.Identity.IsAuthenticated ?
			db.FindUserArticle(slug, this.User.GetUserAccountId()) :
			db.FindArticle(slug)
		);
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ListComments(string slug) {
			var comments = db
				.ListComments(db.FindArticle(slug).Id)
				.Select(c => new CommentThread(c))
				.ToArray();
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
				var userArticle = db.GetUserArticle(binder.ArticleId, this.User.GetUserAccountId());
				if (userArticle.IsRead) {
					var comment = db.CreateComment(WebUtility.HtmlEncode(binder.Text), binder.ArticleId, binder.ParentCommentId, this.User.GetUserAccountId());
					if (binder.ParentCommentId.HasValue) {
						var parent = db.GetComment(binder.ParentCommentId.Value);
						if (parent.UserAccountId != this.User.GetUserAccountId()) {
							var parentUserAccount = db.GetUserAccount(parent.UserAccountId);
							if (parentUserAccount.ReceiveReplyEmailNotifications && parentUserAccount.IsEmailConfirmed) {
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
			return BadRequest();
		}
		[HttpPost]
		public IActionResult UserDelete([FromBody] ArticleIdBinder binder) {
			var userAccountId = this.User.GetUserAccountId();
			var article = db.GetUserArticle(binder.ArticleId, userAccountId);
			if (article.DateStarred.HasValue) {
				db.UnstarArticle(userAccountId, article.Id);
			}
			if (article.DateCreated.HasValue) {
				db.DeleteUserArticle(article.Id, userAccountId);
			}
			return Ok();
		}
		[HttpGet]
		public IActionResult ListReplies(int pageNumber) => Json(PageResult<CommentThread>.Create(
			source: db.ListReplies(this.User.GetUserAccountId(), pageNumber, 40),
			map: comments => comments.Select(c => new CommentThread(c))
		));
		[HttpPost]
		public IActionResult ReadReply([FromBody] ReadReplyBinder binder) {
			var comment = db.GetComment(binder.CommentId);
			if (db.GetComment(comment.ParentCommentId.Value).UserAccountId == User.GetUserAccountId()) {
				db.ReadComment(comment.Id);
				return Ok();
			}
			return BadRequest();
		}
		[HttpPost]
		public IActionResult Star([FromBody] ArticleIdBinder binder) {
			db.StarArticle(this.User.GetUserAccountId(), binder.ArticleId);
			return Ok();
		}
		[HttpPost]
		public IActionResult Unstar([FromBody] ArticleIdBinder binder) {
			db.UnstarArticle(this.User.GetUserAccountId(), binder.ArticleId);
			return Ok();
		}
	}
}