using System;
using System.Linq;
using System.Threading.Tasks;
using api.Analytics;
using api.Authentication;
using api.Configuration;
using api.Cookies;
using api.DataAccess;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.Embed {
	public class EmbedController : Controller {
		private readonly AuthenticationOptions authenticationOptions;
		private readonly CookieOptions cookieOptions;
		private readonly DatabaseOptions databaseOptions;
		private readonly EmbedOptions embedOptions;
		private readonly ILogger<EmbedController> logger;
		private readonly TokenizationOptions tokenizationOptions;
		public EmbedController(
			IOptions<AuthenticationOptions> authenticationOptions,
			IOptions<CookieOptions> cookieOptions,
			IOptions<DatabaseOptions> databaseOptions,
			IOptions<EmbedOptions> embedOptions,
			ILogger<EmbedController> logger,
			IOptions<TokenizationOptions> tokenizationOptions
		) {
			this.authenticationOptions = authenticationOptions.Value;
			this.cookieOptions = cookieOptions.Value;
			this.databaseOptions = databaseOptions.Value;
			this.embedOptions = embedOptions.Value;
			this.logger = logger;
			this.tokenizationOptions = tokenizationOptions.Value;
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Initialization(
			[FromBody] InitializationRequest request
		) {
			// check for extension
			if (Request.Cookies.GetExtensionVersionCookieValue() != null) {
				return Json(
					new InitializationDeactivationResponse()
				);
			}
			// check the host url
			try {
				var articleUrl = new Uri(request.Url);
				if (
					!embedOptions.AllowedHosts.Any(
						allowedHost => articleUrl.Host == allowedHost
					)
				) {
					throw new Exception();
				}
			} catch (Exception) {
				return Problem("Invalid article url.", statusCode: 400);
			}
			// make sure the user has a sessionId
			if (Request.Cookies.GetSessionIdCookieValue() == null) {
				Response.Cookies.SetSessionIdCookie(cookieOptions);
			}
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// don't allow new articles to be created from the embed
				var page = db.FindPage(request.Url);
				if (page == null) {
					return Problem(statusCode: 403);
				}
				// first check for a signed in user
				if (User.Identity.IsAuthenticated) {
					var userAccountId = User.GetUserAccountId();
					var userAccount = await db.GetUserAccountById(userAccountId);
					var userArticle = db.GetUserArticle(
						articleId: page.ArticleId,
						userAccountId: userAccountId
					);
					if (userArticle == null) {
						userArticle = db.CreateUserArticle(
							articleId: page.ArticleId,
							userAccountId: userAccountId,
							readableWordCount: page.WordCount,
							analytics: this.GetClientAnalytics()
						);
					}
					return Json(
						new InitializationActivationResponse(
							article: await db.GetArticleById(
								articleId: page.ArticleId,
								userAccountId: userAccountId
							),
							user: userAccount,
							userArticle: userArticle
						)
					);
				} else {
					// check for a provisional user
					var provisionalUserAccountId = Request.Cookies.GetProvisionalSessionKeyCookieValue(tokenizationOptions);
					if (!provisionalUserAccountId.HasValue) {
						// create a new provisional user
						var provisionalUserAccount = await db.CreateProvisionalUserAccount(
							new UserAccountProvisionalCreationAnalytics() {
								IpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
								UserAgent = Request.Headers["User-Agent"]
							}
						);
						provisionalUserAccountId = provisionalUserAccount.Id;
						Response.Cookies.SetProvisionalSessionKeyCookie(provisionalUserAccount.Id, tokenizationOptions, cookieOptions);
					}
					var userArticle = await db.GetProvisionalUserArticle(
						articleId: page.ArticleId,
						provisionalUserAccountId: provisionalUserAccountId.Value
					);
					if (userArticle == null) {
						userArticle = await db.CreateProvisionalUserArticle(
							articleId: page.ArticleId,
							provisionalUserAccountId: provisionalUserAccountId.Value,
							readableWordCount: page.WordCount,
							analytics: this.GetClientAnalytics()
						);
					}
					return Json(
						new InitializationActivationResponse(
							article: await db.GetArticleForProvisionalUser(
								articleId: page.ArticleId,
								provisionalUserAccountId: provisionalUserAccountId.Value
							),
							user: null,
							userArticle: new UserArticle() {
								ArticleId = userArticle.ArticleId,
								DateCompleted = userArticle.DateCompleted,
								DateCreated = userArticle.DateCreated,
								LastModified = userArticle.LastModified,
								ReadableWordCount = userArticle.ReadableWordCount,
								ReadState = userArticle.ReadState,
								WordsRead = userArticle.WordsRead
							}
						)
					);
				}
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> ReadProgress(
			[FromBody] ReadProgressRequest request
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				if (User.Identity.IsAuthenticated) {
					var userAccountId = User.GetUserAccountId();
					var userArticle = db.GetUserArticle(request.ArticleId, userAccountId);
					db.UpdateReadProgress(
						userArticleId: userArticle.Id,
						readState: request.ReadState,
						analytics: this.GetClientAnalytics()
					);
					return Json(
						new ReadProgressResponse(
							await db.GetArticleById(userArticle.ArticleId, userAccountId)
						)
					);
				} else {
					var provisionalUserAccountId = Request.Cookies.GetProvisionalSessionKeyCookieValue(tokenizationOptions);
					if (!provisionalUserAccountId.HasValue) {
						return Problem(statusCode: 401);
					}
					await db.UpdateProvisionalReadProgress(
						provisionalUserAccountId: provisionalUserAccountId.Value,
						articleId: request.ArticleId,
						readState: request.ReadState,
						analytics: this.GetClientAnalytics()
					);
					return Json(
						new ReadProgressResponse(
							await db.GetArticleForProvisionalUser(
								articleId: request.ArticleId,
								provisionalUserAccountId: provisionalUserAccountId.Value
							)
						)
					);
				}
			}
		}
	}
}