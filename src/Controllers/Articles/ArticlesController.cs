using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using api.DataAccess.Models;

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
				return Json(db.ListUserArticles(userAccount?.Id, minCommentCount: 1));
			}
		}
		[HttpGet]
		public IActionResult UserList() {
			using (var db = new DbConnection()) {
				return Json(db.ListUserArticles(db.GetSession(this.GetSessionKey()).UserAccountId, minPercentComplete: 1));
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
				return Json(db.FindUserArticle(slug, userAccount?.Id));
			}
		}
		[HttpGet]
		public IActionResult ListComments(string slug) {
			using (var db = new DbConnection()) {
				return Json(db.ListComments(db.FindUserArticle(slug).Id));
			}
		}
		[HttpPost]
		public IActionResult PostComment([FromBody] PostCommentBinder binder) {
			using (var db = new DbConnection()) {
				db.CreateComment(binder.Text, binder.ArticleId, db.GetSession(this.GetSessionKey()).UserAccountId);
				return Ok();
			}
		}
	}
}