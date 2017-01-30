using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using api.DataAccess.Models;
using System;

namespace api.Controllers.Articles {
	public class ArticlesController : Controller {
		[HttpGet]
		public IActionResult List() {
			using (var db = new DbConnection()) {
				var sessionKey = this.GetSessionKey();
				UserAccount userAccount;
				if (sessionKey != null) {
					userAccount = db.GetUserAccount(db.GetSession(sessionKey).UserAccountId);
				} else {
					userAccount = null;
				}
				return Json(db.ListArticlesWithComments(userAccount?.Id));
			}
		}
		[HttpGet]
		public IActionResult UserList() {
			using (var db = new DbConnection()) {
				return Json(db.ListUserArticles(db.GetSession(this.GetSessionKey()).UserAccountId));
			}
		}
		[HttpGet]
		public IActionResult Details(string slug) {
			using (var db = new DbConnection()) {
				var sessionKey = this.GetSessionKey();
				UserAccount userAccount;
				if (sessionKey != null) {
					userAccount = db.GetUserAccount(db.GetSession(sessionKey).UserAccountId);
				} else {
					userAccount = null;
				}
				return Json(db.FindArticle(slug, userAccount?.Id));
			}
		}
		[HttpGet]
		public IActionResult ListComments(string slug) {
			using (var db = new DbConnection()) {
				return Json(db.ListComments(db.FindArticle(slug).Id));
			}
		}
		[HttpPost]
		public IActionResult PostComment([FromBody] PostCommentParams param) {
			using (var db = new DbConnection()) {
				db.CreateComment(param.Text, param.ArticleId, db.GetSession(this.GetSessionKey()).UserAccountId);
				return Ok();
			}
		}
	}
}