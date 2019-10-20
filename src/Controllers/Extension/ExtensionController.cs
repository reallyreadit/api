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
using api.BackwardsCompatibility;
using api.Notifications;

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
		public async Task<IActionResult> UserArticle(
			[FromServices] ReadingVerificationService verificationService,
			long id
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				return Json(
					verificationService.AssignProofToken(
						await db.GetArticle(id, userAccountId),
						userAccountId
					)
				);
			}
		}
		[HttpPost]
		public async Task<IActionResult> GetUserArticle(
			[FromServices] ReadingVerificationService verificationService,
			[FromBody] PageInfoBinder binder
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = this.User.GetUserAccountId();
				var page = db.FindPage(binder.Url);
				UserArticle userArticle;
				if (page != null) {
					// update the page if either the wordCount or readableWordCount has increased.
					// we're assuming that the article has been updated with additional text
					// and always storing the largest counts in the global record.
					if (
						binder.WordCount > page.WordCount ||
						binder.ReadableWordCount > page.ReadableWordCount
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
						userArticle = db.CreateUserArticle(
							articleId: page.ArticleId,
							userAccountId: userAccountId,
							readableWordCount: userReadableWordCount,
							analytics: this.GetRequestAnalytics()
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
					userArticle = db.CreateUserArticle(
						articleId: page.ArticleId,
						userAccountId: userAccountId,
						readableWordCount: binder.ReadableWordCount,
						analytics: this.GetRequestAnalytics()
					);
				}
				if (binder.Star) {
					db.StarArticle(userAccountId, page.ArticleId);
				}
				return Json(new {
					UserArticle = verificationService.AssignProofToken(
						await db.GetArticle(page.ArticleId, userAccountId),
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
					User = (
						this.ClientVersionIsGreaterThanOrEqualTo(
							versions: new Dictionary<ClientType, SemanticVersion>() {
								{ ClientType.IosApp, new SemanticVersion(5, 0, 0) },
								{ ClientType.IosExtension, new SemanticVersion(5, 0, 0) },
								{ ClientType.WebExtension, new SemanticVersion(0, 0, 0) }
							}
						) ?
							await db.GetUserAccountById(
								userAccountId: userAccountId
							) :
							new UserAccount_1_2_0(
								user: await db.GetUserAccountById(
									userAccountId: userAccountId
								)
							) as Object
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
					db.UpdateReadProgress(	
						userArticleId: binder.UserPageId,
						readState: binder.ReadState,
						analytics: this.GetRequestAnalytics()
					);
					return Json(
						verificationService.AssignProofToken(
							await db.GetArticle(userArticle.ArticleId, userAccountId),
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
						await db.GetArticle(binder.ArticleId, userAccountId),
						userAccountId
					)
				);
			}
		}
		[HttpPost]
		public async Task<IActionResult> SendInstructions([FromServices] EmailService emailService) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				await emailService.SendExtensionInstructionsEmail(
					recipient: await db.GetUserAccountById(this.User.GetUserAccountId())
				);
			}
			return Ok();
		}
		[HttpGet]
		public async Task<IActionResult> Notifications(
			[FromServices] ObfuscationService obfuscation,
			string[] ids
		) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var clientReceiptIds = ids
					.Select(id => obfuscation.Decode(id))
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
						sinceDate: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(2)),
						excludedReceiptIds: clientReceiptIds
					);
					foreach (var notification in newNotifications) {
						string title, message;
						switch (notification.EventType) {
							case NotificationEventType.Reply:
								var comment = await db.GetComment(notification.CommentIds.Single());
								title = $"{comment.UserAccount} replied to your comment on {comment.ArticleTitle}";
								message = "Click here to view the reply in the comment thread.";
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
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var receiptId = obfuscationService.Decode(id).Value;
				var notification = await db.GetNotification(receiptId);
				ViewActionResource resource;
				long resourceId;
				switch (notification.EventType) {
					case NotificationEventType.Aotd:
						resource = ViewActionResource.Article;
						resourceId = notification.ArticleIds.Single();
						break;
					case NotificationEventType.Reply:
					case NotificationEventType.Loopback:
						resource = ViewActionResource.Comment;
						resourceId = notification.CommentIds.Single();
						break;
					case NotificationEventType.Post:
						if (notification.CommentIds.Any()) {
							resource = ViewActionResource.CommentPost;
							resourceId = notification.CommentIds.Single();
						} else {
							resource = ViewActionResource.SilentPost;
							resourceId = notification.SilentPostIds.Single();
						}
						break;
					case NotificationEventType.Follower:
						resource = ViewActionResource.Follower;
						resourceId = notification.FollowingIds.Single();
						break;
					default:
						throw new InvalidOperationException($"Unexpected value for {nameof(notification.EventType)}");
				}
				return Redirect(
					url: await notificationService.CreateViewInteraction(
						db: db,
						notification: notification,
						channel: NotificationChannel.Extension,
						resource: resource,
						resourceId: resourceId
					)
				);
			}
		}
		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Install(
			[FromServices] IOptions<TokenizationOptions> tokenizationOptions,
			[FromBody] InstallationForm form
		) {
			using (var db = new NpgsqlConnection(this.dbOpts.ConnectionString)) {
				var installationId = Guid.NewGuid();
				await db.LogExtensionInstallation(
					installationId: installationId,
					userAccountId: this.User.GetUserAccountIdOrDefault(),
					platform: form.Os + "/" + form.Arch
				);
				return Json(new {
					installationId = StringEncryption.Encrypt(
						text: installationId.ToString(),
						key: tokenizationOptions.Value.EncryptionKey
					)
				});
			}
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