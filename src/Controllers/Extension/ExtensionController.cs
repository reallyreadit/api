using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using System.Linq;
using api.DataAccess.Models;
using System;
using System.Text.RegularExpressions;

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
		public IActionResult GetUserArticle([FromBody] PageInfoBinder binder) {
			using (var db = new DbConnection()) {
				var userAccountId = db.GetSession(this.GetSessionKey()).UserAccountId;
				var page = db.FindPage(binder.Url);
				UserPage userPage;
				if (page != null) {
					userPage = db.GetUserPage(page.Id, userAccountId);
					if (userPage == null) {
						userPage = db.CreateUserPage(page.Id, userAccountId);
					}
				} else {
					// create article
					var source = db.FindSource(new Uri(binder.Url).Host);
					var slug = source.Slug + "_" + Regex.Replace(Regex.Replace(binder.Article.Title, @"[^a-zA-Z0-9-\s]", ""), @"\s", "-").ToLower();
					var article = db.CreateArticle(
						title: binder.Article.Title,
						slug: slug.Length > 80 ? slug.Substring(0, 80) : slug,
						author: binder.Article.Author,
						datePublished: binder.Article.DatePublished,
						sourceId: source.Id
					);
					page = db.CreatePage(article.Id, binder.Number, binder.WordCount, binder.Url);
					if (binder.Article.PageLinks != null) {
						foreach (var pageLink in binder.Article.PageLinks.Where(p => p.Number != binder.Number)) {
							db.CreatePage(article.Id, pageLink.Number, 0, pageLink.Url);
						}
					}
					// create user page
					userPage = db.CreateUserPage(page.Id, userAccountId);
				}
				return Json(new {
					UserArticle = db.GetUserArticle(page.ArticleId, userAccountId),
					UserPage = userPage
				});
			}
		}
		[HttpPost]
		public IActionResult CommitReadState([FromBody] CommitReadStateBinder binder) {
			using (var db = new DbConnection()) {
				var userPage = db.UpdateUserPage(binder.UserPageId, binder.ReadState);
				return Json(db.GetUserArticle(db.GetPage(userPage.PageId).ArticleId, db.GetSession(this.GetSessionKey()).UserAccountId));
			}
		}
	}
}