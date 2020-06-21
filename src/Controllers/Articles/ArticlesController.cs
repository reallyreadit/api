using Microsoft.AspNetCore.Mvc;
using api.DataAccess;
using api.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using Microsoft.Extensions.Options;
using api.Configuration;
using System.Threading.Tasks;
using api.DataAccess.Models;
using Npgsql;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using api.ReadingVerification;
using api.Analytics;
using api.Notifications;
using api.Encryption;
using api.Routing;
using api.Formatting;

namespace api.Controllers.Articles {
	public class ArticlesController : Controller {
		private DatabaseOptions dbOpts;
		private readonly ILogger<ArticlesController> log;
		public ArticlesController(IOptions<DatabaseOptions> dbOpts, ILogger<ArticlesController> log) {
			this.dbOpts = dbOpts.Value;
			this.log = log;
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> SetAotd(
			[FromServices] IOptions<AuthenticationOptions> authOptions,
			[FromServices] NotificationService notifications,
			[FromForm] AotdForm form
		) {
			if (form.ApiKey == authOptions.Value.ApiKey) {
				using (
					var db = new NpgsqlConnection(
						connectionString: dbOpts.ConnectionString
					)
				) {
					var article = await db.SetAotd();
					await notifications.CreateAotdNotifications(article);
					return Ok();
				}
			}
			return BadRequest();
		}
		// Deprecated 2018-12-18
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> ListHotTopics(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int pageSize
		) {
			return await CommunityReads(verificationService, pageNumber, pageSize, CommunityReadSort.Hot);
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> CommunityReads(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int pageSize,
			CommunityReadSort sort,
			int? minLength = null,
			int? maxLength = null
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountIdOrDefault();
				PageResult<Article> articles;
				switch (sort) {
					case CommunityReadSort.Hot:
						articles = await db.GetHotArticles(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: pageSize,
							minLength: minLength,
							maxLength: maxLength
						);
						break;
					case CommunityReadSort.Top:
						articles = await db.GetTopArticles(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: pageSize,
							minLength: minLength,
							maxLength: maxLength
						);
						break;
					case CommunityReadSort.New:
						articles = await db.GetNewAotdContenders(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: pageSize,
							minLength: minLength,
							maxLength: maxLength
						);
						break;
					default:
						throw new ArgumentException($"Unexpected value for {nameof(sort)}");
				}
				var aotd = (await db.GetAotds(1, userAccountId)).Single();
				var userReadCount = (
					userAccountId.HasValue ?
						await db.GetUserReadCount(userAccountId: userAccountId.Value) :
						0
				);
				if (this.ClientVersionIsGreaterThanOrEqualTo(
					new Dictionary<ClientType, SemanticVersion>() {
						{ ClientType.WebAppServer, new SemanticVersion("1.4.0") },
						{ ClientType.WebAppClient, new SemanticVersion("1.4.0") }
					}
				)) {
					return Json(
						new {
							Aotd = aotd,
							AotdHasAlert = (
								userAccountId.HasValue ?
									(	
										await db.GetUserAccountById(
											userAccountId: userAccountId.Value
										)
									)
									.AotdAlert :
									false
							),
							Articles = articles,
							UserReadCount = userReadCount
						}
					);
				} else {
					return Json(
						new {
							Aotd = aotd,
							Articles = articles,
							UserStats = new {
								ReadCount = userReadCount
							}
						}
					);
				}
			}
		}
		[HttpGet]
		public IActionResult ListStarred(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int? minLength = null,
			int? maxLength = null
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					PageResult<Article>.Create(
						db.GetStarredArticles(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: 40,
							minLength: minLength,
							maxLength: maxLength
						),
						articles => articles.Select(article => verificationService.AssignProofToken(article, userAccountId))
					)
				);
			}
		}
		[HttpGet]
		public async Task<IActionResult> ListHistory(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int? minLength = null,
			int? maxLength = null
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					PageResult<Article>.Create(
						await db.GetArticleHistory(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: 40,
							minLength: minLength,
							maxLength: maxLength
						),
						articles => articles.Select(article => verificationService.AssignProofToken(article, userAccountId))
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public IActionResult Details(
			[FromServices] ReadingVerificationService verificationService,
			string slug
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountIdOrDefault();
				var article = db.FindArticle(slug, userAccountId);
				if (article == null) {
					log.LogError("Article lookup failed. Slug: {Slug}", slug);
					return BadRequest(
						new[] { "Article not found." }
					);
				}
				return Json(
					userAccountId.HasValue ?
						verificationService.AssignProofToken(
							article: article,
							userAccountId: userAccountId.Value
						) :
						article
				);
			}
		}
		[HttpPost]
		public async Task<IActionResult> Star(
			[FromBody] ArticleIdBinder binder,
			[FromServices] ReadingVerificationService verificationService
		) {
			var userAccountId = this.User.GetUserAccountId();
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.StarArticle(userAccountId, binder.ArticleId);
				return Json(verificationService.AssignProofToken(
					article: await db.GetArticle(binder.ArticleId, userAccountId),
					userAccountId: userAccountId
				));
			}
		}
		[HttpPost]
		public async Task<IActionResult> Unstar(
			[FromBody] ArticleIdBinder binder,
			[FromServices] ReadingVerificationService verificationService
		) {
			var userAccountId = this.User.GetUserAccountId();
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				db.UnstarArticle(userAccountId, binder.ArticleId);
				return Json(verificationService.AssignProofToken(
					article: await db.GetArticle(binder.ArticleId, userAccountId),
					userAccountId: userAccountId
				));
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> VerifyProofToken(
			[FromServices] ReadingVerificationService verificationService,
			string token
		) {
			var tokenData = verificationService.GetTokenData(token);
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var readerName = (await db.GetUserAccountById(tokenData.UserAccountId)).Name;
				Article article;
				if (this.User.Identity.IsAuthenticated) {
					var userAccountId = this.User.GetUserAccountId();
					article = verificationService.AssignProofToken(
						await db.GetArticle(tokenData.ArticleId, userAccountId),
						userAccountId
					);
				} else {
					article = await db.GetArticle(tokenData.ArticleId, null);
				}
				return Json(new {
					Article = article,
					ReaderName = readerName
				});
			}
		}
		[HttpPost]
		public async Task<IActionResult> Rate(
			[FromBody] ArticleRatingForm form,
			[FromServices] ReadingVerificationService verificationService
		) {
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var rating = await db.RateArticle(
					articleId: form.ArticleId,
					userAccountId: userAccountId,
					score: form.Score
				);
				if (
					this.ClientVersionIsGreaterThanOrEqualTo(new Dictionary<ClientType, SemanticVersion>() {
						{ ClientType.WebAppClient, new SemanticVersion("1.0.0") },
						{ ClientType.WebExtension, new SemanticVersion("1.0.0") },
						{ ClientType.IosApp, new SemanticVersion("3.1.1") }
					})
				) {
					return Json(new {
						Article = verificationService.AssignProofToken(
							article: await db.GetArticle(form.ArticleId, userAccountId),
							userAccountId: userAccountId
						),
						Rating = rating
					});
				} else {
					return Json(rating);
				}
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> AotdHistory(
			[FromServices] ReadingVerificationService verificationService,
			[FromQuery] ArticleQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await db.GetAotdHistory(
						userAccountId: this.User.GetUserAccountIdOrDefault(),
						pageNumber: query.PageNumber,
						pageSize: 40,
						minLength: query.MinLength,
						maxLength: query.MaxLength
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Author(
			[FromQuery] AuthorArticleQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await db.GetArticlesByAuthorSlug(
						slug: query.Slug,
						userAccountId: this.User.GetUserAccountIdOrDefault(),
						pageNumber: query.PageNumber,
						pageSize: query.PageSize,
						minLength: query.MinLength,
						maxLength: query.MaxLength
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Publisher(
			[FromQuery] PublisherArticleQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await db.GetArticlesBySourceSlug(
						slug: query.Slug,
						userAccountId: this.User.GetUserAccountIdOrDefault(),
						pageNumber: query.PageNumber,
						pageSize: query.PageSize,
						minLength: query.MinLength,
						maxLength: query.MaxLength
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> TwitterCardMetadata(
			[FromServices] ObfuscationService obfuscationService,
			[FromQuery] TwitterCardMetadataRequest request
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var article = db.FindArticle(request.Slug, null);
				if (article == null) {
					log.LogError("Article lookup failed. Slug: {Slug}", request.Slug);
					return BadRequest(
						new[] { "Article not found." }
					);
				}
				string title;
				if (!String.IsNullOrWhiteSpace(request.PostId)) {
					var decodedPostIdParameter = obfuscationService.Decode(request.PostId);
					long userAccountId;
					if (decodedPostIdParameter.Length == 1) {
						var comment = await db.GetComment(
							commentId: decodedPostIdParameter[0]
						);
						userAccountId = comment.UserAccountId;
					} else if (
						decodedPostIdParameter.Length == 2 &&
						decodedPostIdParameter[0] == RoutingService.CommentsUrlSilentPostIdKey
					) {
						var silentPost = await db.GetSilentPost(
							id: decodedPostIdParameter[1]
						);
						userAccountId = silentPost.UserAccountId;
					} else {
						return BadRequest();
					}
					var user = await db.GetUserAccountById(userAccountId);
					title = $"{user.Name} read “{article.Title.RemoveControlCharacters()}”";
				} else {
					title = $"Comments on “{article.Title.RemoveControlCharacters()}” • Readup";
				}
				return Json(
					new TwitterCardMetadata(
						title: title,
						description: "Read comments from verified readers on Readup.",
						imageUrl: (await db.GetArticleImage(article.Id))?.Url
					)
				);
			}
		}
	}
}