using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using api.DataAccess.Models;
using api.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers.Articles {
	public class ArticlesController : Controller {
		[AllowAnonymous]
		[HttpGet]
		public IActionResult List() {
			using (var db = new DbConnection()) {
				return Json(db.ListUserArticles(this.User.GetUserAccountIdOrDefault(), minCommentCount: 1));
			}
		}
		[HttpGet]
		public IActionResult UserList() {
			using (var db = new DbConnection()) {
				return Json(db.ListUserArticles(this.User.GetUserAccountId(), minPercentComplete: 1));
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult Details(string slug) {
			using (var db = new DbConnection()) {
				return Json(db.FindUserArticle(slug, this.User.GetUserAccountIdOrDefault()));
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult ListComments(string slug) {
			using (var db = new DbConnection()) {
				return Json(db.ListComments(db.FindUserArticle(slug).Id));
			}
		}
		[HttpPost]
		public IActionResult PostComment([FromBody] PostCommentBinder binder) {
			using (var db = new DbConnection()) {
				db.CreateComment(binder.Text, binder.ArticleId, this.User.GetUserAccountId());
				return Ok();
			}
		}
	}
}