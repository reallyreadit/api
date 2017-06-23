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
		[AllowAnonymous]
		[HttpGet]
		public IActionResult List() => Json(db.ListUserArticles(
			userAccountId: this.User.GetUserAccountIdOrDefault(),
			minCommentCount: 1,
			sort: ListUserArticlesSort.LastComment
		));
		[HttpGet]
		public IActionResult UserList() => Json(db.ListUserArticles(
			userAccountId: this.User.GetUserAccountId(),
			minPercentComplete: 1,
			sort: ListUserArticlesSort.DateCreated
		));
		[AllowAnonymous]
		[HttpGet]
		public IActionResult Details(string slug) {
			return Json(db.FindUserArticle(slug, this.User.GetUserAccountIdOrDefault()));
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ListComments(string slug) {
			var comments = db
				.ListComments(db.FindUserArticle(slug).Id)
				.Select(c => new CommentThread(c))
				.ToArray();
			foreach (var comment in comments) {
				if (comment.ParentCommentId.HasValue) {
					var siblings = comments.Single(c => c.Id == comment.ParentCommentId).Children;
					siblings.Insert(Math.Max(siblings.IndexOf(siblings.FirstOrDefault(c => c.DateCreated < comment.DateCreated)), 0), comment);
				}
			}
			return Json(comments
				.Where(c => !c.ParentCommentId.HasValue)
				.OrderByDescending(c => c.DateCreated));
		}
		[HttpPost]
		public async Task<IActionResult> PostComment(
			[FromBody] PostCommentBinder binder,
			[FromServices] EmailService emailService,
			[FromServices] IOptions<ReadingParametersOptions> readingParametersOpts
		) {
			var userArticle = db.GetUserArticle(binder.ArticleId, this.User.GetUserAccountId());
			if (userArticle.PercentComplete >= readingParametersOpts.Value.ArticleUnlockThreshold) {
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
			return BadRequest();
		}
		[HttpPost]
		public IActionResult UserDelete([FromBody] UserDeleteBinder binder) {
			db.DeleteUserArticle(binder.ArticleId, this.User.GetUserAccountId());
			return Ok();
		}
		[HttpGet]
		public IActionResult ListReplies() {
			return Json(db.ListReplies(this.User.GetUserAccountId()).Select(c => new CommentThread(c)));
		}
		[HttpPost]
		public IActionResult ReadReply([FromBody] ReadReplyBinder binder) {
			var comment = db.GetComment(binder.CommentId);
			if (db.GetComment(comment.ParentCommentId.Value).UserAccountId == User.GetUserAccountId()) {
				db.ReadComment(comment.Id);
				return Ok();
			}
			return BadRequest();
		}
	}
}