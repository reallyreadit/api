using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using System.Linq;
using api.DataAccess.Models;

namespace api.Controllers.Extension {
	public class ExtensionController : Controller {
		[HttpGet]
		public IActionResult FindSource(string hostname) {
			using (var db = new DbConnection()) {
				return Json(db.FindSource(hostname));
			}
		}
		[HttpGet]
		public IActionResult GetOptions() {
			return Json(new {
				EventPageOptions = new {
					ArticleUnlockThreshold = 90
				},
				ContentScriptOptions = new {
					WordReadRate = 100,
					PageOffsetUpdateRate = 2000,
					ReadStateCommitRate = 2000,
					UrlCheckRate = 2500
				}
			});
		}
		[HttpPost]
		public IActionResult Commit([FromBody] CommitArticleParams param) {
			using (var db = new DbConnection()) {
				var userAccount = db.GetUserAccount(db.GetSession(this.GetSessionKey()).UserAccountId);
				var userPage = db.FindUserPage(param.Slug, param.PageNumber, userAccount.Id);
				if (userPage != null) {
					// compare
					if (param.PercentComplete >= userPage.PercentComplete) {
						// update
						db.UpdateUserPage(userPage.Id, param.ReadState, param.PercentComplete);
					} else {
						// return newer copy
						return Json(new {
							ReadState = userPage.ReadState,
							PercentComplete = userPage.PercentComplete
						});
					}
				} else {
					var article = db.FindArticle(param.Slug);
					Page page;
					if (article == null) {
						// create article
						article = db.CreateArticle(
							title: param.Title,
							slug: param.Slug,
							author: param.Author,
							datePublished: param.DatePublished,
							sourceId: param.SourceId
						);
						// create pages
						page = db.CreatePage(article.Id, param.PageNumber, param.WordCount, param.Url);
						foreach (var pageLink in param.PageLinks.Where(p => p.PageNumber != param.PageNumber)) {
							db.CreatePage(article.Id, pageLink.PageNumber, 0, pageLink.Url);
						}
					} else {
						page = db.GetPage(article.Id, param.PageNumber);
					}
					// create user page
					db.CreateUserPage(page.Id, userAccount.Id, param.ReadState, param.PercentComplete);
				}
				return Ok();
			}
		}
	}
}