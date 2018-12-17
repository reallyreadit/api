using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using System.Linq;
using api.DataAccess.Models;
using System;
using System.Text.RegularExpressions;
using api.Authentication;
using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using api.Configuration;
using Npgsql;
using System.Threading.Tasks;
using api.Messaging;
using api.ReadingVerification;

namespace api.Controllers.Extension {
	public class ExtensionController : Controller {
		private DatabaseOptions dbOpts;
		public ExtensionController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		private static string CreateSlug(string value) {
			var slug = Regex.Replace(Regex.Replace(value, @"[^a-zA-Z0-9-\s]", ""), @"\s", "-").ToLower();
			return slug.Length > 80 ? slug.Substring(0, 80) : slug;
		}
		private static string PrepareArticleTitle(string title) {
			// return if null
			if (title == null) {
				return title;
			}
			// trim whitespace
			title = title.Trim();
			// check for double title
			if (title.Length > 2 && title.Length % 2 == 0) {
				var firstHalf = title.Substring(0, title.Length / 2);
				if (firstHalf == title.Substring(title.Length / 2)) {
					title = firstHalf;
				}
			}
			return title;
		}
		private static DateTime? ParseArticleDate(string dateString) {
			DateTime date;
			if (DateTime.TryParse(dateString, out date)) {
				return date;
			}
			if (DateTime.TryParseExact(dateString, new[] { "MMMM d \"at\" h:mm tt" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) {
				return date;
			}
			return null;
		}
		private static string Decode(string text) {
			text = WebUtility.HtmlDecode(text);
			text = WebUtility.UrlDecode(text);
			return text;
		}
		[HttpGet]
		public IActionResult FindSource(string hostname) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.FindSource(hostname));
			}
		}
		[HttpGet]
		public IActionResult UserArticle(
			[FromServices] ReadingVerificationService verificationService,
			long id
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					verificationService.AssignProofToken(
						db.GetUserArticle(id, userAccountId),
						userAccountId
					)
				);
			}
		}
		[HttpPost]
		public IActionResult GetUserArticle(
			[FromServices] ReadingVerificationService verificationService,
			[FromBody] PageInfoBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
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
					Uri pageUri = new Uri(binder.Url), sourceUri;
					if (!Uri.TryCreate(binder.Article.Source.Url, UriKind.Absolute, out sourceUri)) {
						sourceUri = new Uri(pageUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped));
					}
					var source = db.FindSource(sourceUri.Host);
					if (source == null) {
						// create source
						var sourceName = Decode(binder.Article.Source.Name) ?? Regex.Replace(sourceUri.Host, @"^www\.", String.Empty);
						source = db.CreateSource(
							name: sourceName,
							url: sourceUri.ToString(),
							hostname: sourceUri.Host,
							slug: CreateSlug(sourceName)
						);
					}
					var title = PrepareArticleTitle(Decode(binder.Article.Title));
					// temp workaround to circumvent npgsql type mapping bug
					var authors = binder.Article.Authors.Distinct().ToArray();
					var articleId = db.CreateArticle(
						title,
						slug: source.Slug + "_" + CreateSlug(title),
						sourceId: source.Id,
						datePublished: ParseArticleDate(binder.Article.DatePublished),
						dateModified: ParseArticleDate(binder.Article.DateModified),
						section: Decode(binder.Article.Section),
						description: Decode(binder.Article.Description),
						authorNames: authors.Select(author => author.Name).ToArray(),
						authorUrls: authors.Select(author => author.Url).ToArray(),
						tags: binder.Article.Tags.Distinct().ToArray()
					);
					page = db.CreatePage(articleId, binder.Number ?? 1, binder.WordCount, binder.ReadableWordCount, binder.Url);
					foreach (var pageLink in binder.Article.PageLinks.Where(p => p.Number != page.Number)) {
						db.CreatePage(articleId, pageLink.Number, 0, 0, pageLink.Url);
					}
					// create user page
					userPage = db.CreateUserPage(page.Id, userAccountId);
				}
				return Json(new {
					UserArticle = verificationService.AssignProofToken(
						db.GetUserArticle(page.ArticleId, userAccountId),
						userAccountId
					),
					UserPage = userPage
				});
			}
		}
		[HttpPost]
		public IActionResult CommitReadState(
			[FromServices] ReadingVerificationService verificationService,
			[FromBody] CommitReadStateBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userPage = db.UpdateUserPage(binder.UserPageId, binder.ReadState);
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					verificationService.AssignProofToken(
						db.GetUserArticle(db.GetPage(userPage.PageId).ArticleId, userAccountId),
						userAccountId
					)
				);
			}
		}
		[HttpGet]
		public IActionResult GetSourceRules() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.GetSourceRules());
			}
		}
		[HttpPost]
		public IActionResult SetStarred(
			[FromServices] ReadingVerificationService verificationService,
			[FromBody] SetStarredBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				if (binder.IsStarred) {
					db.StarArticle(userAccountId, binder.ArticleId);
				} else {
					db.UnstarArticle(userAccountId, binder.ArticleId);
				}
				return Json(
					verificationService.AssignProofToken(
						db.GetUserArticle(binder.ArticleId, userAccountId),
						userAccountId
					)
				);
			}
		}
		[HttpPost]
		public async Task<IActionResult> SendInstructions([FromServices] EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await emailService.SendExtensionInstructionsEmail(
					recipient: db.GetUserAccount(this.User.GetUserAccountId())
				);
			}
			return Ok();
		}
	}
}