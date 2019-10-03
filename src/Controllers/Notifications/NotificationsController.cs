using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using api.DataAccess;
using api.Authentication;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using api.Notifications;
using System;
using api.Encryption;

namespace api.Controllers.Notifications {
	public class NotificationsController : Controller {
		private readonly DatabaseOptions dbOpts;
		public NotificationsController(
			IOptions<DatabaseOptions> dbOpts
		) {
			this.dbOpts = dbOpts.Value;
		}
		[HttpPost]
		public async Task<IActionResult> ClearAlerts(
			[FromBody] ClearAlertForm form
		) {
			using (
				var db = new NpgsqlConnection(
					connectionString: dbOpts.ConnectionString
				)
			) {
				NotificationEventType[] types;
				var userAccountId = User.GetUserAccountId();
				switch (form.Alert) {
					case Alert.Aotd:
						types = new NotificationEventType[0];
						await db.ClearAotdAlert(
							userAccountId: userAccountId
						);	
						break;
					case Alert.Followers:
						types = new [] {
							NotificationEventType.Follower
						};
						break;
					case Alert.Following:
						types = new [] {
							NotificationEventType.Post
						};
						break;
					case Alert.Inbox:
						types = new [] {
							NotificationEventType.Reply,
							NotificationEventType.Loopback
						};
						break;
					default:
						return BadRequest();
				}
				foreach (var type in types) {
					await db.ClearAllAlerts(
						type: type,
						userAccountId: userAccountId
					);
				}
				return Ok();
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Index(
			[FromServices] IOptions<ServiceEndpointsOptions> endpoints,
			[FromServices] NotificationService notifications,
			[FromServices] ObfuscationService obfuscation,
			string tokenString
		) {
			var token = notifications.DecryptTokenString(
				tokenString: tokenString
			);
			if (token.Channel.HasValue && token.Action.HasValue) {
				using (
					var db = new NpgsqlConnection(
						connectionString: dbOpts.ConnectionString
					)
				) {
					var notification = await db.GetNotification(
						receiptId: token.ReceiptId
					);
					switch (notification.EventType) {
						case NotificationEventType.Aotd:
						case NotificationEventType.Follower:
						case NotificationEventType.Loopback:
						case NotificationEventType.Post:
						case NotificationEventType.Reply:
							if (
								(
									token.Action == NotificationAction.View ||
									token.Action == NotificationAction.Reply
								) &&
								!notification.DateAlertCleared.HasValue
							) {
								await db.ClearAlert(
									receiptId: token.ReceiptId
								);
							}
							break;
					}
					switch (token.Action) {
						case NotificationAction.Open:
							try {
								await db.CreateNotificationInteraction(
									receiptId: token.ReceiptId,
									channel: token.Channel.Value,
									action: token.Action.Value
								);
							} catch (NpgsqlException ex) when (
								String.Equals(ex.Data["ConstraintName"], "notification_interaction_unique_open")
							) {
								// swallow duplicate open exception
							}
							return File(
								fileContents: Convert.FromBase64String(
									s: "R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw=="
								),
								contentType: "image/gif"
							);
						case NotificationAction.View:
							string path;
							switch (token.ViewActionResource) {
								case ViewActionResource.Article:
								case ViewActionResource.Comments:
								case ViewActionResource.Comment:
									long articleId;
									long? commentId;
									string pathRoot;
									if (token.ViewActionResource == ViewActionResource.Comment) {
										articleId = (
												await db.GetComment(
													commentId: token.ViewActionResourceId.Value
												)
											)
											.ArticleId;
										commentId = token.ViewActionResourceId;
										pathRoot = "comments";
									} else {
										articleId = token.ViewActionResourceId.Value;
										commentId = null;
										pathRoot = (
											token.ViewActionResource == ViewActionResource.Article ?
												"read" :
												"comments"
										);
									}
									var slugParts = (
											await db.GetArticle(
												articleId: articleId
											)
										)
										.Slug.Split('_');
									path = $"/{pathRoot}/{slugParts[0]}/{slugParts[1]}";
									if (commentId.HasValue) {
										path += "/" + obfuscation.Encode(
											number: commentId.Value
										);
									}
									break;
								case ViewActionResource.CommentPost:
									path = $"/following/comment/{obfuscation.Encode(token.ViewActionResourceId.Value)}";
									break;
								case ViewActionResource.SilentPost:
									path = $"/following/post/{obfuscation.Encode(token.ViewActionResourceId.Value)}";
									break;
								case ViewActionResource.Follower:
									var userName = (
											await db.GetUserAccountById(
												userAccountId: (
													await db.GetFollowing(
														followingId: token.ViewActionResourceId.Value
													)
												)
												.FollowerUserAccountId
											)
										)
										.Name;
									path = $"/profile?followers&userName={userName}";
									break;
								default:
									return BadRequest();
							}
							var url = endpoints.Value.WebServer.CreateUrl(
								path: path
							);
							try {
								await db.CreateNotificationInteraction(
									receiptId: token.ReceiptId,
									channel: token.Channel.Value,
									action: token.Action.Value,
									url: url
								);
							} catch (NpgsqlException ex) when (
								String.Equals(ex.Data["ConstraintName"], "notification_interaction_unique_view")
							) {
								// swallow duplicate view exception
							}
							return Redirect(
								url: url
							);
					}
				}
			}
			return BadRequest();
		}
	}
}