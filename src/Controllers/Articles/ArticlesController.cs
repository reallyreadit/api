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
using api.Authorization;

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
				Article article;
				using (
					var db = new NpgsqlConnection(
						connectionString: dbOpts.ConnectionString
					)
				) {
					var aotdId = await db.SetAotd();
					article = await db.GetArticleById(articleId: aotdId, userAccountId: null);
				}
				await notifications.CreateAotdNotifications(article);
				return Ok();
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
				PageResult<long> articleIds;
				switch (sort) {
					case CommunityReadSort.Hot:
						articleIds = await db.GetHotArticles(
							pageNumber: pageNumber,
							pageSize: pageSize,
							minLength: minLength,
							maxLength: maxLength
						);
						break;
					case CommunityReadSort.Top:
						articleIds = await db.GetTopArticles(
							pageNumber: pageNumber,
							pageSize: pageSize,
							minLength: minLength,
							maxLength: maxLength
						);
						break;
					case CommunityReadSort.New:
						articleIds = await db.GetNewAotdContenders(
							pageNumber: pageNumber,
							pageSize: pageSize,
							minLength: minLength,
							maxLength: maxLength
						);
						break;
					default:
						throw new ArgumentException($"Unexpected value for {nameof(sort)}");
				}
				var articles = await PageResult<Article>.CreateAsync(
					articleIds,
					async articleIds => await db.GetArticlesAsync(
						articleIds: articleIds.ToArray(),
						userAccountId: userAccountId
					)
				);
				var aotd = await db.GetArticleById(
					articleId: (await db.GetAotds(1)).Single(),
					userAccountId: userAccountId
				);
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
		public async Task<IActionResult> ListStarred(
			[FromServices] ReadingVerificationService verificationService,
			int pageNumber,
			int? minLength = null,
			int? maxLength = null
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					await PageResult<Article>.CreateAsync(
						await db.GetStarredArticles(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: 40,
							minLength: minLength,
							maxLength: maxLength
						),
						async articleIds => (
								await db.GetArticlesAsync(
									articleIds: articleIds.ToArray(),
									userAccountId: userAccountId
								)
							)
							.Select(article => verificationService.AssignProofToken(article, userAccountId))
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
					await PageResult<Article>.CreateAsync(
						await db.GetArticleHistory(
							userAccountId: userAccountId,
							pageNumber: pageNumber,
							pageSize: 40,
							minLength: minLength,
							maxLength: maxLength
						),
						async articleIds => (
								await db.GetArticlesAsync(
									articleIds: articleIds.ToArray(),
									userAccountId: userAccountId
								)
							)
							.Select(article => verificationService.AssignProofToken(article, userAccountId))
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Details(
			[FromServices] ReadingVerificationService verificationService,
			string slug
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountIdOrDefault();
				var article = await db.GetArticleBySlug(slug, userAccountId);
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
					article: await db.GetArticleById(binder.ArticleId, userAccountId),
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
					article: await db.GetArticleById(binder.ArticleId, userAccountId),
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
						await db.GetArticleById(tokenData.ArticleId, userAccountId),
						userAccountId
					);
				} else {
					article = await db.GetArticleById(tokenData.ArticleId, null);
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
							article: await db.GetArticleById(form.ArticleId, userAccountId),
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
					await PageResult<Article>.CreateAsync(
						await db.GetAotdHistory(
							pageNumber: query.PageNumber,
							pageSize: 40,
							minLength: query.MinLength,
							maxLength: query.MaxLength
						),
						async articleIds => await db.GetArticlesAsync(
							articleIds: articleIds.ToArray(),
							userAccountId: this.User.GetUserAccountIdOrDefault()
						)
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
					await PageResult<Article>.CreateAsync(
						await db.GetArticlesByAuthorSlug(
							slug: query.Slug,
							pageNumber: query.PageNumber,
							pageSize: query.PageSize,
							minLength: query.MinLength,
							maxLength: query.MaxLength
						),
						async articleIds => await db.GetArticlesAsync(
							articleIds: articleIds.ToArray(),
							userAccountId: this.User.GetUserAccountIdOrDefault()
						)
					)
				);
			}
		}
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		[HttpPost]
		public async Task<IActionResult> AuthorAssignment(
			[FromBody] AuthorAssignmentRequest request
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				// First find the article.
				var article = await db.GetArticleBySlug(slug: request.ArticleSlug, userAccountId: null);
				if (article == null) {
					return Problem("Article not found.", statusCode: 404);
				}
				// Then create the author slug and look for an existing author.
				var sanitizer = new StringSanitizer();
				var authorSlug = sanitizer.GenerateSlug(request.AuthorName);
				var author = await db.GetAuthor(slug: authorSlug);
				if (author == null) {
					// Create a new author.
					try {
						author = await db.CreateAuthorAsync(
							name: sanitizer.SanitizeSingleLine(request.AuthorName),
							slug: authorSlug
						);
					} catch (Exception ex) {
						log.LogError(ex, "Failed to create new author during manual assignment.");
						return Problem("Failed to create new author.", statusCode: 500);
					}
				}
				// Finally assign the author to the article.
				try {
					var assignment = await db.AssignAuthorToArticleAsync(
						articleId: article.Id,
						authorId: author.Id,
						assignedByUserAccountId: User.GetUserAccountId()
					);
					if (assignment == null) {
						return Problem("Author already assigned to article.", statusCode: 500);
					}
				} catch (Exception ex) {
					log.LogError(ex, "Failed to manually assign author to article.");
					return Problem("Failed to assign author to article.", statusCode: 500);
				}
			}
			return Ok();
		}
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		[HttpPost]
		public async Task<IActionResult> AuthorUnassignment(
			[FromBody] AuthorUnassignmentRequest request
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				// First find the article.
				var article = await db.GetArticleBySlug(slug: request.ArticleSlug, userAccountId: null);
				if (article == null) {
					return Problem("Article not found.", statusCode: 404);
				}
				// Then find the author.
				var author = await db.GetAuthor(slug: request.AuthorSlug);
				if (author == null) {
					return Problem("Author not found.", statusCode: 404);
				}
				// Finally unassign the author.
				try {
					var assignment = await db.UnassignAuthorFromArticleAsync(
						articleId: article.Id,
						authorId: author.Id,
						unassignedByUserAccountId: User.GetUserAccountId()
					);
					if (assignment == null) {
						return Problem("Author not found for article.", statusCode: 500);
					}
				} catch (Exception ex) {
					log.LogError(ex, "Failed to unassign author from article.");
					return Problem("Failed to unassign author from article.", statusCode: 500);
				}
			}
			return Ok();
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Publisher(
			[FromQuery] PublisherArticleQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await PageResult<Article>.CreateAsync(
						await db.GetArticlesBySourceSlug(
							slug: query.Slug,
							pageNumber: query.PageNumber,
							pageSize: query.PageSize,
							minLength: query.MinLength,
							maxLength: query.MaxLength
						),
						async articleIds => await db.GetArticlesAsync(
							articleIds: articleIds.ToArray(),
							userAccountId: this.User.GetUserAccountIdOrDefault()
						)
					)
				);
			}
		}
		[HttpPost]
		public async Task<IActionResult> Search(
			[FromBody] SearchQuery query
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(
					await PageResult<Article>.CreateAsync(
						await db.SearchArticles(
							pageNumber: 1,
							pageSize: 40,
							sourceSlugs: query.Sources,
							authorSlugs: query.Authors,
							tagSlugs: query.Tags,
							minLength: query.MinLength,
							maxLength: query.MaxLength
						),
						async articleIds => await db.GetArticlesAsync(
							articleIds: articleIds.ToArray(),
							userAccountId: User.GetUserAccountId()
						)
					)
				);
			}
		}
		[HttpGet]
		public async Task<IActionResult> SearchOptions() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var searchOptions = (await db.GetSearchOptions())
					.GroupBy(
						option => option.Category
					);
				return Json(
					new {
						Authors = searchOptions
							.Single(
								group => group.Key == "author"
							)
							.OrderByDescending(
								option => option.Score
							)
							.Select(
								option => new {
									option.Name,
									option.Score,
									option.Slug
								}
							),
						Sources = searchOptions
							.Single(
								group => group.Key == "source"
							)
							.OrderByDescending(
								option => option.Score
							)
							.Select(
								option => new {
									option.Name,
									option.Score,
									option.Slug
								}
							),
						Tags = searchOptions
							.Single(
								group => group.Key == "tag"
							)
							.OrderByDescending(
								option => option.Score
							)
							.Select(
								option => new {
									option.Name,
									option.Score,
									option.Slug
								}
							)
					}
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
				var article = await db.GetArticleBySlug(request.Slug, null);
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