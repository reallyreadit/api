using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using System.Linq;
using api.DataAccess.Models;
using System;
using System.Text.RegularExpressions;
using api.Authentication;

namespace api.Controllers.Extension {
	public class ExtensionController : Controller {
		private static string CreateSlug(string value) {
			var slug = Regex.Replace(Regex.Replace(value, @"[^a-zA-Z0-9-\s]", ""), @"\s", "-").ToLower();
			return slug.Length > 80 ? slug.Substring(0, 80) : slug;
		}
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
				var userAccountId = this.User.GetUserAccountId();
				var page = db.FindPage(binder.Url);
				UserPage userPage;
				if (page != null) {
					userPage = db.GetUserPage(page.Id, userAccountId);
					if (userPage == null) {
						userPage = db.CreateUserPage(page.Id, userAccountId);
					}
				} else {
					// create article
					var pageUri = new Uri(binder.Url);
					var source = db.FindSource(pageUri.Host);
					if (source == null) {
						// create source
						Uri sourceUri;
						if (!Uri.TryCreate(binder.Article.Source.Url, UriKind.Absolute, out sourceUri)) {
							sourceUri = new Uri(pageUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped));
						}
						var sourceName = binder.Article.Source.Name ?? sourceUri.Host;
						source = db.CreateSource(
							name: sourceName,
							url: sourceUri.ToString(),
							hostname: sourceUri.Host,
							slug: CreateSlug(sourceName)
						);
					}
					var article = db.CreateArticle(
						title: binder.Article.Title,
						slug: source.Slug + "_" + CreateSlug(binder.Article.Title),
						sourceId: source.Id,
						datePublished: binder.Article.DatePublished,
						dateModified: binder.Article.DateModified,
						section: binder.Article.Section,
						description: binder.Article.Description,
						authors: binder.Article.Authors,
						tags: binder.Article.Tags
					);
					page = db.CreatePage(article.Id, binder.Number ?? 1, binder.WordCount, binder.Url);
					foreach (var pageLink in binder.Article.PageLinks.Where(p => p.Number != page.Number)) {
						db.CreatePage(article.Id, pageLink.Number, 0, pageLink.Url);
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
				return Json(db.GetUserArticle(db.GetPage(userPage.PageId).ArticleId, this.User.GetUserAccountId()));
			}
		}
	}
}