using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using api.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Net;
using Microsoft.Extensions.Options;
using api.Configuration;

namespace api.Controllers.Articles {
	public class ArticlesController : Controller {
		private DbConnection db;
		public ArticlesController(DbConnection db) {
			this.db = db;
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult List() {
			return Json(db.ListUserArticles(this.User.GetUserAccountIdOrDefault(), minCommentCount: 1));
		}
		[HttpGet]
		public IActionResult UserList() {
			return Json(db.ListUserArticles(this.User.GetUserAccountId(), minPercentComplete: 1));
		}
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
		public IActionResult PostComment([FromBody] PostCommentBinder binder) {
			return Json(new CommentThread(db.CreateComment(WebUtility.HtmlEncode(binder.Text), binder.ArticleId, binder.ParentCommentId, this.User.GetUserAccountId())));
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
	}
}