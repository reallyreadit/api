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
using api.Analytics;
using Microsoft.AspNetCore.Authorization;
using api.Encryption;
using System.Collections.Generic;
using api.Notifications;
using api.Formatting;
using Microsoft.Extensions.Logging;
using api.Cookies;
using api.Errors;

namespace api.Controllers.Extension {
	public class ExtensionController : Controller {
		private DatabaseOptions dbOpts;
		private readonly ILogger<ExtensionController> logger;
		public ExtensionController(
			IOptions<DatabaseOptions> dbOpts,
			ILogger<ExtensionController> logger
		) {
			this.dbOpts = dbOpts.Value;
			this.logger = logger;
		}
		private static PageInfoBinder.ArticleBinder.AuthorBinder[] AssignMissingAuthors(PageInfoBinder.ArticleBinder.AuthorBinder[] authors, string sourceHost) {
			if (authors.Any()) {
				return authors;
			}
			var assignments = new Dictionary<string, string>() {
				{
					"aaronzlewis.com",
					"Aaron Z. Lewis"
				},
				{
					"alexarohn.com",
					"Alexa Rohn"
				},
				{
					"attentionactivist.com",
					"Jay Vidyarthi"
				},
				{
					"blog.viktomas.com",
					"Tomas Vik"
				},
				{
					"contrapoints.com",
					"Natalie Wynn"
				},
				{
					"franklywrite.com",
					"Cynthia Franks"
				},
				{
					"kevin-indig.com",
					"Kevin Indig"
				},
				{
					"stratechery.com",
					"Ben Thompson"
				},
				{
					"waitbutwhy.com",
					"Tim Urban"
				},
				{
					"seths.blog",
					"Seth Godin"
				},
				{
					"paulgraham.com",
					"Paul Graham"
				},
				{
					"raptitude.com",
					"David Cain"
				},
				{
					"manassaloi.com",
					"Manas J. Saloi"
				},
				{
					"calnewport.com",
					"Cal Newport"
				},
				{
					"jamesclear.com",
					"James Clear"
				},
				{
					"dreamtimehealings.com",
					"Helen Knight"
				},
				{
					"nav.al",
					"Naval Ravikant"
				},
				{
					"craigmod.com",
					"Craig Mod"
				},
				{
					"dougantin.com",
					"Doug Antin"
				},
				{
					"lisarichardsonbylines.com",
					"Lisa Richardson"
				},
				{
					"perell.com",
					"David Perell"
				}
			};
			var matchingKey = assignments.Keys.SingleOrDefault(
				key => key == sourceHost
			);
			if (matchingKey == null) {
				return authors;
			}
			return new PageInfoBinder.ArticleBinder.AuthorBinder[] {
				new PageInfoBinder.ArticleBinder.AuthorBinder() {
					Name = assignments[matchingKey]
				}
			};
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
			// remove control characters
			title = title.RemoveControlCharacters();
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
		private static string PrepareArticleSection(string section) {
			// we should do more sanitization here but for now just prevent exceptions from overly long values
			var sectionLimit = 256;
			if (section?.Length > sectionLimit) {
				return section.Substring(0, sectionLimit);
			}
			return section;
		}
		private static AuthorMetadata[] PrepareAuthors(PageInfoBinder.ArticleBinder.AuthorBinder[] authors) {
			// check for null parameter or names
			authors = (authors ?? new PageInfoBinder.ArticleBinder.AuthorBinder[0])
				.Where(
					author => !String.IsNullOrWhiteSpace(author.Name)
				)
				.ToArray();
			var sanitizer = new StringSanitizer();
			return authors
				// prepare author names for sanitized single line
				.Select(
					author => new {
						Name = sanitizer.SanitizeSingleLine(author.Name),
						author.Url
					}
				)
				// ensure name contains at least one word char
				.Where(
					author => Regex.IsMatch(author.Name, @"\w")
				)
				// generate slug
				.Select(
					author => new AuthorMetadata(
						name: author.Name,
						url: author.Url,
						slug: sanitizer.GenerateSlug(author.Name)
					)
				)
				// de-duplicate
				.GroupBy(
					author => author.Slug
				)
				.Select(
					group => group
						.OrderByDescending(
							author => author.Url?.Length ?? 0
						)
						.ThenByDescending(
							author => author.Name.Length
						)
						.First()
				)
				.ToArray();
		}
		private static TagMetadata[] PrepareTags(string[] tagStrings) {
			// check for null parameter or names
			tagStrings = (tagStrings ?? new string[0])
				.Where(
					tag => !String.IsNullOrWhiteSpace(tag)
				)
				// split comma tags
				.SelectMany(
					tag => tag.Count(
							c => c == ','
						) > 1 ?
							Regex.Split(tag, @",(?![^(]*\))") :
							new[] {
								tag
							}
				)
				.Select(
					tag => tag.Trim()
				)
				// remove prefixes
				.Select(
					tag => Regex.IsMatch(tag, @"^\W*t(ag|opic)(?!\s*\w)", RegexOptions.IgnoreCase) ?
						Regex.Replace(tag, @"^\W*t(ag|opic)\W*", String.Empty, RegexOptions.IgnoreCase) :
						tag
				)
				.ToArray();
			// sanitize
			var sanitizer = new StringSanitizer();
			var tags = tagStrings
				// prepare tags for sanitized single line
				.Select(sanitizer.SanitizeSingleLine)
				// ensure tag contains at least one word char
				.Where(
					tag => Regex.IsMatch(tag, @"\w")
				)
				// generate slug
				.Select(
					tag => new TagMetadata(
						name: tag,
						slug: sanitizer.GenerateSlug(tag)
					)
				)
				.ToArray();
			// blacklist and merge
			var slugBlacklistRegexes = new[] {
				@"^cnbc$",
				@"^elevated\-false$",
				@"^layercake\-",
				@"^lite\-true$",
				@"^lockedpostsource\-",
				@"^make\-it$",
				@"^makeit$",
				@"^source\-tagname\-cnbc\-us\-source$"
			};
			tags = tags
				.Where(
					tag => !slugBlacklistRegexes.Any(
						regex => Regex.IsMatch(tag.Slug, regex)
					)
				)
				.ToArray();
			var slugMergers = new[] {
				new {
					Sources = new[] {
						"coronavirus-2019-ncov",
						"covid-19",
						"coronavirus-outbreak"
					},
					Target = "coronavirus"
				},
				new {
					Sources = new[] {
						"tech"
					},
					Target = "technology"
				},
				new {
					Sources = new[] {
						"trump-donald-j",
						"trump",
						"donald-j-trump"
					},
					Target = "donald-trump"
				},
				new {
					Sources = new[] {
						"type-news"
					},
					Target = "news"
				},
				new {
					Sources = new[] {
						"facebook-inc"
					},
					Target = "facebook"
				},
				new {
					Sources = new[] {
						"startup"
					},
					Target = "startups"
				},
				new {
					Sources = new[] {
						"us"
					},
					Target = "united-states"
				}
			};
			foreach (var tag in tags) {
				var matchingMerger = slugMergers.SingleOrDefault(
					merger => merger.Sources.Contains(tag.Slug)
				);
				if (matchingMerger != null) {
					tag.Slug = matchingMerger.Target;
				}
			}
			return tags
				// de-duplicate
				.GroupBy(
					tag => tag.Slug
				)
				.Select(
					group => group
						.OrderByDescending(
							tag => tag.Name.Length
						)
						.First()
				)
				.ToArray();
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
		private IActionResult DuplicateSourceError(Uri sourceUri, string sourceName, string sourceSlug) {
			if (
				this.ClientVersionIsGreaterThanOrEqualTo(
					new Dictionary<ClientType, SemanticVersion>() {
							{ ClientType.IosExtension, new SemanticVersion(0, 0, 0) },
							{ ClientType.WebExtension, new SemanticVersion(3, 0, 0) }
					}
				)
			) {
				logger.LogError(
					"Duplicate source. Name: {Name} Url: {Url} Hostname: {Hostname} Slug: {Slug}",
					sourceName,
					sourceUri.ToString(),
					sourceUri.Host,
					sourceSlug
				);
			}
			return BadRequest(
				new[] {
					"DuplicateSource"
				}
			);
		}
		[HttpGet]
		public IActionResult FindSource(string hostname) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.FindSource(hostname));
			}
		}
		[HttpGet]
		public async Task<IActionResult> UserArticle(
			[FromServices] ReadingVerificationService verificationService,
			long id
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					verificationService.AssignProofToken(
						await db.GetArticleById(id, userAccountId),
						userAccountId
					)
				);
			}
		}
		[HttpGet]
		public IActionResult Blacklist() {
			return Json(
				new [] {
					@"^https://www\.fastmail\.com/",
					@"^https://docs\.google\.com/",
					@"^https://mail\.google\.com/",
					@"^https://.+\.substack\.com/$"
				}
			);
		}
		[HttpPost]
		public async Task<IActionResult> GetUserArticle(
			[FromServices] ReadingVerificationService verificationService,
			[FromBody] PageInfoBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				// resolve the source first so we can search for duplicate articles by slug
				Uri pageUri = new Uri(binder.Url), sourceUri;
				if (!Uri.TryCreate(binder.Article.Source.Url, UriKind.Absolute, out sourceUri)) {
					sourceUri = new Uri(pageUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped));
				}
				var sourceHost = Regex.Replace(sourceUri.Host, @"^www\.", String.Empty);
				var source = db.FindSource(sourceHost);
				if (source == null) {
					// create source
					string
						sourceName = Decode(binder.Article.Source.Name) ?? sourceHost,
						sourceSlug = CreateSlug(sourceName);
					try {
						source = db.CreateSource(
							name: sourceName,
							url: sourceUri.ToString(),
							hostname: sourceHost,
							slug: sourceSlug
						);
					} catch (PostgresException ex) when (
						ex.ConstraintName == "source_hostname_key" ||
						ex.ConstraintName == "source_slug_key"
					) {
						if (ex.ConstraintName == "source_slug_key") {
							sourceSlug = CreateSlug(sourceHost);
							try {
								source = db.CreateSource(
									name: sourceName,
									url: sourceUri.ToString(),
									hostname: sourceHost,
									slug: sourceSlug
								);
							} catch (PostgresException retryEx) when (
								retryEx.ConstraintName == "source_hostname_key" ||
								retryEx.ConstraintName == "source_slug_key"
							) {
								return DuplicateSourceError(
									sourceUri: sourceUri,
									sourceName: sourceName,
									sourceSlug: sourceSlug
								);
							}
						} else {
							return DuplicateSourceError(
								sourceUri: sourceUri,
								sourceName: sourceName,
								sourceSlug: sourceSlug
							);
						}
					}
				}
				// create the slug
				string
					title = PrepareArticleTitle(Decode(binder.Article.Title)),
					slug = source.Slug + "_" + CreateSlug(title);
				// look for existing page
				var page = db.FindPage(binder.Url);
				if (page == null) {
					var article = await db.GetArticleBySlug(slug, userAccountId);
					if (article != null) {
						page = db.FindPage(article.Url);
					}
				}
				UserArticle userArticle;
				var clientAnalytics = this.GetClientAnalytics();
				// This endpoint is currently used by both the native client reader and the native client share extension.
				// A user article should only be marked as having been viewed if this endpoint was called by a reader.
				var markArticleAsViewed = clientAnalytics.Type == ClientType.DesktopApp || clientAnalytics.Type == ClientType.IosApp;
				if (page != null) {
					// update the page if either the wordCount or readableWordCount has increased.
					// we're assuming that the article has been updated with additional text
					// and always storing the largest counts in the global record.
					// 2020-01-11: this is causing some serious issues due to the extension parser
					// picking up lots of extra noise. only increase the word count if the current
					// count is 2 minutes or less to fix soft paywalled articles
					if (
						(
							binder.WordCount > page.WordCount ||
							binder.ReadableWordCount > page.ReadableWordCount
						) &&
						page.WordCount <= (184 * 2)
					) {
						page = db.UpdatePage(
							pageId: page.Id,
							wordCount: Math.Max(binder.WordCount, page.WordCount),
							readableWordCount: Math.Max(binder.ReadableWordCount, page.ReadableWordCount)
						);
					}
					// decide if we're using the global record readableWordCount or the one from this parse result
					int userReadableWordCount;
					if (
						binder.ReadableWordCount < page.ReadableWordCount &&
						binder.ReadableWordCount >= (page.ReadableWordCount * 0.80)
					) {
						userReadableWordCount = binder.ReadableWordCount;
					} else {
						userReadableWordCount = page.ReadableWordCount;
					}
					// either create the user article if it doesn't exist or update it
					// as long as it won't erase any read words from the existing read state
					userArticle = db.GetUserArticle(page.ArticleId, userAccountId);
					if (userArticle == null) {
						userArticle = await db.CreateUserArticle(
							articleId: page.ArticleId,
							userAccountId: userAccountId,
							readableWordCount: userReadableWordCount,
							markAsViewed: markArticleAsViewed,
							analytics: clientAnalytics
						);
					} else if (
						!userArticle.DateCompleted.HasValue &&
						userArticle.ReadableWordCount != userReadableWordCount
					) {
						var readClusters = userArticle.ReadState.Last() > 0 ?
							userArticle.ReadState :
							userArticle.ReadState.Length > 1 ?
								userArticle.ReadState
									.Take(userArticle.ReadState.Length - 1)
									.ToArray() :
								new int[0];
						var readClustersWordCount = readClusters.Sum(cluster => Math.Abs(cluster));
						if (userReadableWordCount >= readClustersWordCount) {
							int[] newReadState;
							if (!readClusters.Any()) {
								newReadState = new[] { -userReadableWordCount };
							} else if (userReadableWordCount > readClustersWordCount) {
								newReadState = readClusters
									.Append(readClustersWordCount - userReadableWordCount)
									.ToArray();
							} else {
								newReadState = readClusters;
							}
							userArticle = db.UpdateUserArticle(
								userArticleId: userArticle.Id,
								readableWordCount: userReadableWordCount,
								readState: newReadState
							);
						}
					}
				} else {
					// create article
					long articleId;
					try {
						articleId = db.CreateArticle(
							title,
							slug,
							sourceId: source.Id,
							datePublished: ParseArticleDate(binder.Article.DatePublished),
							dateModified: ParseArticleDate(binder.Article.DateModified),
							section: PrepareArticleSection(Decode(binder.Article.Section)),
							description: Decode(binder.Article.Description),
							authors: PrepareAuthors(
								AssignMissingAuthors(binder.Article.Authors, sourceHost)
							),
							tags: PrepareTags(binder.Article.Tags)
						);
					} catch (NpgsqlException ex) when (
						ex.Data.Contains("ConstraintName") &&
						String.Equals(ex.Data["ConstraintName"], "article_slug_key")
					) {
						if (
							this.ClientVersionIsGreaterThanOrEqualTo(
								new Dictionary<ClientType, SemanticVersion>() {
									{ ClientType.IosExtension, new SemanticVersion(0, 0, 0) },
									{ ClientType.WebExtension, new SemanticVersion(3, 0, 0) }
								}
							)
						) {
							logger.LogError(
								"Duplicate article. Title: {Title} Url: {Url} Slug: {Slug}",
								title,
								binder.Url,
								slug
							);
						}
						return BadRequest(
							new [] {
								"DuplicateArticle"
							}
						);
					}
					page = db.CreatePage(
						articleId: articleId,
						number: binder.Number ?? 1,
						wordCount: binder.WordCount,
						readableWordCount: binder.ReadableWordCount,
						url: binder.Url
					);
					foreach (var pageLink in binder.Article.PageLinks.Where(p => p.Number != page.Number)) {
						db.CreatePage(
							articleId: articleId,
							number: pageLink.Number,
							wordCount: 0,
							readableWordCount: binder.ReadableWordCount,
							url: pageLink.Url
						);
					}
					// create user article
					userArticle = await db.CreateUserArticle(
						articleId: page.ArticleId,
						userAccountId: userAccountId,
						readableWordCount: binder.ReadableWordCount,
						markAsViewed: markArticleAsViewed,
						analytics: clientAnalytics
					);
				}
				// check for existing image and set it required
				if (!String.IsNullOrWhiteSpace(binder.Article.ImageUrl)) {
					// make absolute if relative
					var imageUrl = binder.Article.ImageUrl;
					if (!imageUrl.StartsWith("http")) {
						if (imageUrl.StartsWith("//")) {
							imageUrl = Regex.Match(binder.Url, "^https?").Value + ':' + imageUrl;
						} else {
							imageUrl = String.Join(
								'/',
								binder.Url.TrimEnd('/'),
								imageUrl.TrimStart('/')
							);
						}
					}
					if ((await db.GetArticleImage(userArticle.ArticleId))?.Url != imageUrl) {
						try {
							await db.SetArticleImage(
								articleId: userArticle.ArticleId,
								creatorUserId: userAccountId,
								url: imageUrl
							);
						} catch (NpgsqlException ex) when (String.Equals(ex.Data["ConstraintName"], "article_image_pkey")) {
							// ignore duplicate exception
						}
					}
				}
				if (binder.Star) {
					db.StarArticle(userAccountId, page.ArticleId);
				}
				return Json(new {
					UserArticle = verificationService.AssignProofToken(
						await db.GetArticleById(page.ArticleId, userAccountId),
						userAccountId
					),
					// temporarily maintain compatibility with existing clients
					// PageId is an unused property anyway so just set it to 0
					UserPage = new {
						Id = userArticle.Id,
						PageId = 0,
						UserAccountId = userArticle.UserAccountId,
						DateCreated = userArticle.DateCreated,
						LastModified = userArticle.LastModified,
						ReadableWordCount = userArticle.ReadableWordCount,
						ReadState = userArticle.ReadState,
						WordsRead = userArticle.WordsRead,
						DateCompleted = userArticle.DateCompleted
					},
					User = await db.GetUserAccountById(
						userAccountId: userAccountId
					)
				});
			}
		}
		[HttpPost]
		public async Task<IActionResult> CommitReadState(
			[FromServices] ReadingVerificationService verificationService,
			[FromBody] CommitReadStateBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				// prevent users from update articles that don't belong to them
				var userAccountId = this.User.GetUserAccountId();
				// also temporarily maintaining compatibility here
				var userArticle = db.GetUserArticle(binder.UserPageId);
				if (userArticle.UserAccountId == userAccountId) {
					try {
						db.UpdateReadProgress(
							userArticleId: binder.UserPageId,
							readState: binder.ReadState,
							analytics: this.GetClientAnalytics()
						);
					} catch (PostgresException ex) when (ex.Detail == ReadingErrorType.SubscriptionRequired) {
						return Problem(
							statusCode: 403,
							type: ReadingErrorType.SubscriptionRequired,
							title: "Subscription required."
						);
					}
					return Json(
						verificationService.AssignProofToken(
							await db.GetArticleById(userArticle.ArticleId, userAccountId),
							userAccountId
						)
					);
				}
			}
			return BadRequest();
		}
		[HttpGet]
		public IActionResult GetSourceRules() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.GetSourceRules());
			}
		}
		[HttpPost]
		public async Task<IActionResult> SetStarred(
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
						await db.GetArticleById(binder.ArticleId, userAccountId),
						userAccountId
					)
				);
			}
		}
		[HttpGet]
		public async Task<IActionResult> Notifications(
			[FromServices] ObfuscationService obfuscation,
			string[] ids
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var clientReceiptIds = ids
					.Select(id => obfuscation.DecodeSingle(id))
					.OfType<long>()
					.ToArray();
				var userAccountId = User.GetUserAccountId();
				var cleared = (
					ids.Any() ?
						(await db.GetNotifications(clientReceiptIds))
							.Where(notification => notification.DateAlertCleared.HasValue)
							.ToArray() :
						new Notification[0]
				);
				if (!cleared.Any() || cleared.All(notification => notification.UserAccountId == userAccountId)) {
					var created = new List<object>();
					var newNotifications = await db.GetExtensionNotifications(
						userAccountId: userAccountId,
						sinceDate: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)),
						excludedReceiptIds: clientReceiptIds
					);
					foreach (var notification in newNotifications) {
						string title, message;
						switch (notification.EventType) {
							case NotificationEventType.Aotd:
								title = "Article of the Day";
								message = (await db.GetArticleById(notification.ArticleIds.Single(), userAccountId: null)).Title;
								break;
							case NotificationEventType.Follower:
								var follower = await db.GetUserAccountById(
									(await db.GetFollowing(notification.FollowingIds.Single())).FollowerUserAccountId
								);
								title = $"{follower.Name} is now following you.";
								message = "Click here to view";
								break;
							case NotificationEventType.Loopback:
								var loopback = await db.GetComment(notification.CommentIds.Single());
								title = $"{loopback.UserAccount} commented on {loopback.ArticleTitle}";
								message = "Click here to view.";
								break;
							case NotificationEventType.Post:
								string userName, articleTitle;
								if (notification.CommentIds.Any()) {
									var postComment = await db.GetComment(notification.CommentIds.Single());
									userName = postComment.UserAccount;
									articleTitle = postComment.ArticleTitle;
								} else if (notification.SilentPostIds.Any()) {
									var silentPost = await db.GetSilentPost(notification.SilentPostIds.Single());
									userName = (await db.GetUserAccountById(silentPost.UserAccountId)).Name;
									articleTitle = (await db.GetArticleById(silentPost.ArticleId, userAccountId: null)).Title;
								} else {
									throw new ArgumentException("Post notification must reference a comment or silent post");
								}
								title = $"{userName} posted {articleTitle}";
								message = "Click here to view.";
								break;
							case NotificationEventType.Reply:
								var reply = await db.GetComment(notification.CommentIds.Single());
								title = $"{reply.UserAccount} replied to your comment on {reply.ArticleTitle}";
								message = "Click here to view.";
								break;
							default:
								throw new NotSupportedException("Unsupported EventType");
						}
						created.Add(
							new {
								Id = obfuscation.Encode(notification.ReceiptId),
								Title = title,
								Message = message
							}
						);
					}
					return Json(
						data: new {
							Cleared = cleared.Select(notification => obfuscation.Encode(notification.ReceiptId)),
							Created = created,
							User = await db.GetUserAccountById(userAccountId)
						}
					);
				}
				return BadRequest();
			}
		}
		[HttpGet]
		public async Task<IActionResult> Notification(
			[FromServices] ObfuscationService obfuscationService,
			[FromServices] NotificationService notificationService,
			string id
		) {
			return Redirect(
				(
					await notificationService.ProcessExtensionView(
						receiptId: obfuscationService.DecodeSingle(id).Value
					)
				)
				.ToString()
			);
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Install(
			[FromServices] IOptions<CookieOptions> cookieOptions,
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromBody] InstallationForm form
		) {
			// set the extension version cookie on install or update
			Response.Cookies.SetExtensionVersionCookie(
				version: this
					.GetClientAnalytics()
					.Version,
				cookieOptions: cookieOptions.Value
			);
			// set installation id if not present
			Guid? newInstallationId;
			if (String.IsNullOrWhiteSpace(form.InstallationId)) {
				newInstallationId = Guid.NewGuid();
				using (var db = new NpgsqlConnection(this.dbOpts.ConnectionString)) {
					await db.LogExtensionInstallation(
						installationId: newInstallationId.Value,
						userAccountId: this.User.GetUserAccountIdOrDefault(),
						platform: form.Os + "/" + form.Arch
					);
				}
			} else {
				newInstallationId = null;
			}
			// check for installation redirect cookie
			return Json(
				new {
					InstallationId = (
						newInstallationId.HasValue ?
							StringEncryption.Encrypt(
								text: newInstallationId.Value.ToString(),
								key: tokenizationOptions.Value.EncryptionKey
							) :
							null
					),
					RedirectPath = Request.Cookies.GetExtensionInstallationRedirectPathCookieValue()
				}
			);
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Uninstall(
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromBody] RemovalForm form
		) {
			using (var db = new NpgsqlConnection(this.dbOpts.ConnectionString)) {
				await db.LogExtensionRemoval(
					installationId: Guid.Parse(
						StringEncryption.Decrypt(
							text: form.InstallationId,
							key: tokenizationOptions.Value.EncryptionKey
						)
					),
					userAccountId: this.User.GetUserAccountIdOrDefault()
				);
				return Ok();
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> UninstallFeedback(
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromBody] RemovalFeedbackForm form
		) {
			if (!String.IsNullOrWhiteSpace(form.Reason)) {
				using (var db = new NpgsqlConnection(this.dbOpts.ConnectionString)) {
					await db.LogExtensionRemovalFeedback(
						installationId: Guid.Parse(
							StringEncryption.Decrypt(
								text: form.InstallationId,
								key: tokenizationOptions.Value.EncryptionKey
							)
						),
						reason: form.Reason.Trim()
					);
					return Ok();
				}
			}
			return BadRequest();
		}
	}
}