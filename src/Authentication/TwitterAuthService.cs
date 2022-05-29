// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using api.BackgroundProcessing;
using api.Commenting;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.DataAccess.Serialization;
using api.ImageProcessing;
using api.Routing;
using api.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Npgsql;

namespace api.Authentication {
	public class TwitterAuthService {
		private readonly IHttpClientFactory httpClientFactory;
		private readonly TwitterAuthOptions authOptions;
		private readonly DatabaseOptions databaseOptions;
		private readonly ILogger<AppleAuthService> logger;
		private readonly RoutingService routing;
		private readonly IBackgroundTaskQueue taskQueue;
		private readonly TweetImageRenderingService imageService;
		public TwitterAuthService(
			IHttpClientFactory httpClientFactory,
			IOptions<AuthenticationOptions> authOptions,
			IOptions<DatabaseOptions> databaseOptions,
			ILogger<AppleAuthService> logger,
			RoutingService routing,
			IBackgroundTaskQueue taskQueue,
			TweetImageRenderingService imageService
		) {
			this.httpClientFactory = httpClientFactory;
			this.authOptions = authOptions.Value.TwitterAuth;
			this.databaseOptions = databaseOptions.Value;
			this.logger = logger;
			this.routing = routing;
			this.taskQueue = taskQueue;
			this.imageService = imageService;
		}
		private async Task<string> AcquireBotTweetTargets(
			long articleId
		) {
			Source source;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// retrieve the source
				source = await db.GetSourceOfArticle(articleId);
			}
			// search for twitter handle if we haven't already
			if (source.TwitterHandleAssignment == TwitterHandleAssignment.None) {
				var searchResults = await SearchTwitterUsers(source.Name);
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					source = await db.AssignTwitterHandleToSource(
						sourceId: source.Id,
						twitterHandle: searchResults.FirstOrDefault()?.ScreenName,
						twitterHandleAssignment: TwitterHandleAssignment.NameSearch
					);
				}
			}
			// retrieve the authors
			Author[] authors;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				authors = (await db.GetAuthorsOfArticle(articleId)).ToArray();
			}
			// search for twitter handles if there's a reasonable number of authors
			if (authors.Length <= 3) {
				for (var i = 0; i < authors.Length; i++) {
					var author = authors[i];
					if (author.TwitterHandleAssignment != TwitterHandleAssignment.None) {
						continue;
					}
					var searchResults = await SearchTwitterUsers(author.Name + ' ' + source.Name);
					var searchMethod = TwitterHandleAssignment.NameAndCompanySearch;
					if (!searchResults.Any()) {
						searchResults = await SearchTwitterUsers(author.Name);
						searchMethod = TwitterHandleAssignment.NameSearch;
					}
					using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
						authors[i] = await db.AssignTwitterHandleToAuthor(
							authorId: author.Id,
							twitterHandle: searchResults.FirstOrDefault()?.ScreenName,
							twitterHandleAssignment: searchMethod
						);
					}
				}
			} else {
				authors = new Author[0];
			}
			return String.Join(
				' ',
				authors
					.Select(
						author => author.TwitterHandle
					)
					.Where(
						twitterHandle => !String.IsNullOrWhiteSpace(twitterHandle)
					)
					.Select(
						twitterHandle => '@' + twitterHandle
					)
			);
		}
		private string CreateAlphanumericNonce(int byteCount) {
			var bytes = new byte[byteCount];
			using (var rng = RandomNumberGenerator.Create()) {
				rng.GetBytes(bytes);
			}
			return Regex.Replace(
				input: Convert.ToBase64String(bytes),
				pattern: "[^0-9a-zA-Z]",
				replacement: String.Empty
			);
		}
		private string CreateOauthSignature(
			HttpMethod method,
			Uri uri,
			IEnumerable<KeyValuePair<string, string>> parameters,
			string consumerSecret,
			string accessTokenSecret
		) {
			var paramString = String.Join(
				'&',
				PercentEncodeAndOrderKeyValuePairs(parameters)
					.Select(
						encodedKvp => encodedKvp.Key + "=" + encodedKvp.Value
					)
			);
			var signatureBaseString = String.Join(
				'&',
				method.ToString(),
				Uri.EscapeDataString(uri.Scheme + "://" + uri.Host + uri.AbsolutePath),
				Uri.EscapeDataString(paramString)
			);
			var signingKey = Uri.EscapeDataString(consumerSecret) + '&';
			if (!String.IsNullOrWhiteSpace(accessTokenSecret)) {
				signingKey += Uri.EscapeDataString(accessTokenSecret);
			}
			using (
				var hmacSha1 = new HMACSHA1(
					Encoding.UTF8.GetBytes(signingKey)
				)
			) {
				return Convert.ToBase64String(
					hmacSha1.ComputeHash(
						Encoding.UTF8.GetBytes(signatureBaseString)
					)
				);
			}
		}
		private HttpRequestMessage CreateRequestMessage(
			HttpMethod method,
			Uri uri,
			IEnumerable<KeyValuePair<string, string>> queryStringParameters,
			HttpContent bodyContent,
			IDictionary<string, string> oauthParameters,
			ITwitterToken accessToken
		) {
			// check for null arguments
			if (queryStringParameters == null) {
				queryStringParameters = new KeyValuePair<string, string>[0];
			}
			if (oauthParameters == null) {
				oauthParameters = new Dictionary<string, string>();
			}

			// get automatic parameters
			var nonce = CreateAlphanumericNonce(byteCount: 32);
			var timestamp = Math
				.Floor(
					DateTime.UtcNow
						.Subtract(DateTime.UnixEpoch)
						.TotalSeconds
				)
				.ToString();

			// oauth implementation
			var standardOauthParameters = new Dictionary<string, string>() {
				{ "oauth_consumer_key", authOptions.ConsumerKey },
				{ "oauth_nonce", nonce },
				{ "oauth_signature_method", "HMAC-SHA1" },
				{ "oauth_timestamp", timestamp },
				{ "oauth_version", "1.0" }
			};
			if (accessToken != null) {
				oauthParameters.Add("oauth_token", accessToken.OAuthToken);
			}
			var oauthSignature = CreateOauthSignature(
				method: method,
				uri: uri,
				parameters: queryStringParameters
					.Concat(oauthParameters)
					.Concat(standardOauthParameters),
				consumerSecret: authOptions.ConsumerSecret,
				accessTokenSecret: accessToken?.OAuthTokenSecret
			);

			// create the message
			var message = new HttpRequestMessage(method, uri) {
				Content = bodyContent
			};
			message.Headers.Authorization = new AuthenticationHeaderValue(
				"OAuth",
				String.Join(
					", ",
					PercentEncodeAndOrderKeyValuePairs(
							oauthParameters
								.Concat(standardOauthParameters)
								.Concat(
									new[] {
										new KeyValuePair<string, string>("oauth_signature", oauthSignature)
									}
								)
						)
						.Select(
							encodedKvp => encodedKvp.Key + "=\"" + encodedKvp.Value + "\""
						)
				)
			);
			if (queryStringParameters.Any()) {
				var uriBuilder = new UriBuilder(message.RequestUri) {
					Query = String.Join(
						'&',
						PercentEncodeAndOrderKeyValuePairs(queryStringParameters)
							.Select(
								encodedKvp => encodedKvp.Key + "=" + encodedKvp.Value
							)
					)
				};
				message.RequestUri = uriBuilder.Uri;
			}
			return message;
		}
		private string CreateTruncatedTweetText(
			string[] segments,
			int shrinkableSegmentIndex,
			Uri uri
		) {
			var tweetLimit = 280;
			var urlLength = 23;
			var textLimit = tweetLimit - urlLength - 1;
			// clean up segments
			segments = segments
				.Where(
					segment => !String.IsNullOrWhiteSpace(segment)
				)
				.ToArray();
			shrinkableSegmentIndex = Math.Min(shrinkableSegmentIndex, segments.Length - 1);
			var text = String.Join(' ', segments);
			if (text.Length > textLimit) {
				// shrink the shrinkable segment to a minimum of 20 chars plus ellipses
				var shrinkableMinLength = 20;
				var ellipses = "...";
				var shrinkable = segments[shrinkableSegmentIndex];
				if (shrinkable.Length > shrinkableMinLength + ellipses.Length) {
					segments[shrinkableSegmentIndex] = (
						shrinkable.Substring(
							0,
							Math.Min(
								Math.Max(
									shrinkable.Length - (text.Length - textLimit) - ellipses.Length,
									shrinkableMinLength
								),
								shrinkable.Length
							)
						) +
						ellipses
					);
					text = String.Join(' ', segments);
				}
				// if we're still over the limit just truncate the entire text
				if (text.Length > textLimit) {
					text = text.Substring(0, textLimit - ellipses.Length) + ellipses;
				}
			}
			return text + ' ' + uri.ToString();
		}
		private async Task<TwitterAccessToken> GetAccessToken(
			string requestToken,
			string requestVerifier
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Post,
				uri: new Uri(authOptions.TwitterApiServerUrl + "/oauth/access_token"),
				queryStringParameters: null,
				bodyContent: null,
				oauthParameters: new Dictionary<string, string>() {
					{ "oauth_token", requestToken },
					{ "oauth_verifier", requestVerifier }
				},
				accessToken: null
			);
			string responseContent;
			try {
				using (var client = httpClientFactory.CreateClient())
				using (var response = await client.SendAsync(message)) {
					responseContent = await response.Content.ReadAsStringAsync();
					if (!response.IsSuccessStatusCode) {
						logger.LogError("Twitter OAuth verification failed. Status code: {StatusCode} Response: {Content}", response.StatusCode, responseContent);
						return null;
					}
				}
				var form = HttpUtility.ParseQueryString(responseContent);
				return new TwitterAccessToken(
					oauthToken: form["oauth_token"],
					oauthTokenSecret: form["oauth_token_secret"],
					screenName: form["screen_name"],
					userId: form["user_id"]
				);
			} catch (Exception ex) {
				logger.LogError(ex, "HttpClient error during Twitter OAuth verification.");
				return null;
			}
		}
		private async Task<TwitterRequestToken> GetRequestToken(
			string oauthCallback,
			UserAccountCreationAnalytics signUpAnalytics
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Post,
				uri: new Uri(authOptions.TwitterApiServerUrl + "/oauth/request_token"),
				queryStringParameters: null,
				bodyContent: null,
				oauthParameters: new Dictionary<string, string>() {
					{ "oauth_callback", oauthCallback }
				},
				accessToken: null
			);
			string responseContent;
			try {
				using (var client = httpClientFactory.CreateClient())
				using (var response = await client.SendAsync(message)) {
					responseContent = await response.Content.ReadAsStringAsync();
					if (!response.IsSuccessStatusCode) {
						logger.LogError("Twitter OAuth request failed. Status code: {StatusCode} Response: {Content}", response.StatusCode, responseContent);
						return null;
					}
				}
				var form = HttpUtility.ParseQueryString(responseContent);
				if (form["oauth_callback_confirmed"] != "true") {
					logger.LogError("Twitter OAuth request callback not confirmed. Response: {Content}", responseContent);
				}
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					var requestToken = await db.CreateAuthServiceRequestToken(
						provider: AuthServiceProvider.Twitter,
						tokenValue: form["oauth_token"],
						tokenSecret: form["oauth_token_secret"],
						signUpAnalytics: signUpAnalytics
					);
					return new TwitterRequestToken(
						oauthToken: requestToken.TokenValue,
						oauthTokenSecret: requestToken.TokenSecret,
						oauthCallbackConfirmed: true
					);
				}
			} catch (Exception ex) {
				logger.LogError(ex, "HttpClient error during Twitter OAuth token request.");
				return null;
			}
		}
		private async Task<TwitterUser> GetTwitterUser(
			TwitterAccessToken accessToken
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Get,
				uri: new Uri(authOptions.TwitterApiServerUrl + "/1.1/account/verify_credentials.json"),
				queryStringParameters: new Dictionary<string, string>() {
					{ "include_email", "true" },
					{ "include_entities", "false" },
					{ "skip_status", "true" }
				},
				bodyContent: null,
				oauthParameters: null,
				accessToken: accessToken
			);
			string responseContent;
			try {
				using (var client = httpClientFactory.CreateClient())
				using (var response = await client.SendAsync(message)) {
					responseContent = await response.Content.ReadAsStringAsync();
					if (!response.IsSuccessStatusCode) {
						logger.LogError("Twitter credentials verification failed. Status code: {StatusCode} Response: {Content}", response.StatusCode, responseContent);
						return null;
					}
				}
				return JsonSnakeCaseSerializer.Deserialize<TwitterUser>(responseContent);
			} catch (Exception ex) {
				logger.LogError(ex, "HttpClient error during Twitter credentials verification.");
				return null;
			}
		}
		private IOrderedEnumerable<KeyValuePair<string, string>> PercentEncodeAndOrderKeyValuePairs(
			IEnumerable<KeyValuePair<string, string>> keyValuePairs
		) => (
			keyValuePairs
				.Select(
					kvp => new KeyValuePair<string, string>(
						key: Uri.EscapeDataString(kvp.Key),
						value: Uri.EscapeDataString(kvp.Value)
					)
				)
				.OrderBy(
					kvp => kvp.Key
				)
				.ThenBy(
					kvp => kvp.Value
				)
		);
		private async Task<IEnumerable<TwitterUserSearchResult>> SearchTwitterUsers(
			string query
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Get,
				uri: new Uri(authOptions.TwitterApiServerUrl + "/1.1/users/search.json"),
				queryStringParameters: new Dictionary<string, string>() {
					{ "q", query },
					{ "include_entities", "false" }
				},
				bodyContent: null,
				oauthParameters: null,
				accessToken: authOptions.SearchAccount
			);
			string responseContent;
			try {
				using (var client = httpClientFactory.CreateClient())
				using (var response = await client.SendAsync(message)) {
					responseContent = await response.Content.ReadAsStringAsync();
					if (!response.IsSuccessStatusCode) {
						logger.LogError("Twitter user search failed. Status code: {StatusCode} Response: {Content}", response.StatusCode, responseContent);
						return null;
					}
				}
				return JsonSnakeCaseSerializer.Deserialize<TwitterUserSearchResult[]>(responseContent);
			} catch (Exception ex) {
				logger.LogError(ex, "HttpClient error during Twitter user search.");
				return new TwitterUserSearchResult[0];
			}
		}
		private async Task TweetFromLinkedAccounts(
			Comment comment,
			SilentPost silentPost,
			string articleSlug,
			UserAccount user
		) {
			IEnumerable<AuthServiceAccount> twitterAccounts;
			string userTimeZoneTzDbName;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				twitterAccounts = (
						await db.GetAuthServiceAccountsForUserAccount(
							userAccountId: user.Id
						)
					)
					.Where(
						account => account.Provider == AuthServiceProvider.Twitter
					)
					.ToArray();
				if (twitterAccounts.Any() && user.TimeZoneId.HasValue) {
					userTimeZoneTzDbName = (await db.GetTimeZoneById(user.TimeZoneId.Value)).Name;
				} else {
					userTimeZoneTzDbName = null;
				}
			}
			if (twitterAccounts.Any()) {
				taskQueue.QueueBackgroundWorkItem(
					async cancellationToken => {
						string status;
						long? mediaId;
						if (comment != null) {
							status = routing
								.CreateCommentUrl(articleSlug, comment.Id)
								.ToString();
							var orderedAccounts = twitterAccounts.OrderBy(
								account => account.DateIdentityCreated
							);
							var mediaUpload = await UploadImage(
								imageData: imageService.RenderTweet(
									text: comment.Text,
									datePosted: (
										Instant
											.FromDateTimeOffset(new DateTimeOffset(comment.DateCreated, TimeSpan.Zero))
											.InZone(
												DateTimeZoneProviders.Tzdb.GetZoneOrNull(userTimeZoneTzDbName) ??
												DateTimeZoneProviders.Tzdb.GetSystemDefault()
											)
											.ToDateTimeUnspecified()
									),
									userName: user.Name
								),
								additionalOwners: (
									orderedAccounts
										.Skip(1)
										.Select(
											account => account.ProviderUserId
										)
										.ToArray()
								),
								account: orderedAccounts.First()
							);
							mediaId = mediaUpload.MediaId;
						} else {
							status = routing
								.CreateSilentPostUrl(articleSlug, silentPost.Id)
								.ToString();
							mediaId = null;
						}
						var tweets = new List<( long IdentityId, TwitterTweet Tweet )>();
						foreach (var account in twitterAccounts) {
							var tweet = await Tweet(
								status: status,
								mediaIds: (
									mediaId.HasValue ?
										new[] {
											mediaId.Value
										} :
										null
								),
								account: account
							);
							if (tweet != null) {
								tweets.Add(
									(
										account.IdentityId,
										tweet
									)
								);
							}
						}
						if (tweets.Any()) {
							using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
								foreach (var tweet in tweets) {
									await db.CreateAuthServicePost(
										identityId: tweet.IdentityId,
										commentId: comment?.Id,
										silentPostId: silentPost?.Id,
										content: status,
										providerPostId: tweet.Tweet.Id.ToString()
									);
								}
							}
						}
					}
				);
			}
		}
		private async Task<TwitterTweet> Tweet(
			string status,
			long[] mediaIds,
			AuthServiceAccount account
		) {
			var queryStringParameters = new Dictionary<string, string>() {
				{ "status", status }
			};
			if (mediaIds?.Any() ?? false) {
				queryStringParameters.Add("media_ids", String.Join(',', mediaIds));
			}
			var message = CreateRequestMessage(
				method: HttpMethod.Post,
				uri: new Uri(authOptions.TwitterApiServerUrl + "/1.1/statuses/update.json"),
				queryStringParameters: queryStringParameters,
				bodyContent: null,
				oauthParameters: null,
				accessToken: new TwitterToken(
					oauthToken: account.AccessTokenValue,
					oauthTokenSecret: account.AccessTokenSecret
				)
			);
			string responseContent;
			try {
				using (var client = httpClientFactory.CreateClient())
				using (var response = await client.SendAsync(message)) {
					responseContent = await response.Content.ReadAsStringAsync();
					if (!response.IsSuccessStatusCode) {
						logger.LogError("Twitter status update failed. Status code: {StatusCode} Response: {Content}", response.StatusCode, responseContent);
						try {
							var errorResponse = JsonSnakeCaseSerializer.Deserialize<TwitterErrorResponse>(responseContent);
							if (errorResponse.Errors.Any(error => error.Code == 89)) {
								using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
									await db.DisassociateAuthServiceAccount(
										identityId: account.IdentityId
									);
								}
							}
						} catch {
							// swallow
						}
						return null;
					}
				}
				return JsonSnakeCaseSerializer.Deserialize<TwitterTweet>(responseContent);
			} catch (Exception ex) {
				logger.LogError(ex, "HttpClient error during Twitter status update.");
				return null;
			}
		}
		private async Task<TwitterMediaUpload> UploadImage(
			byte[] imageData,
			string[] additionalOwners,
			AuthServiceAccount account
		) {
			var queryStringParameters = new Dictionary<string, string>();
			if (additionalOwners?.Any() ?? false) {
				queryStringParameters.Add("additional_owners", String.Join(',', additionalOwners));
			}
			var message = CreateRequestMessage(
				method: HttpMethod.Post,
				uri: new Uri(authOptions.TwitterUploadServerUrl + "/1.1/media/upload.json"),
				queryStringParameters: queryStringParameters,
				bodyContent: new MultipartFormDataContent() {
					{
						new ByteArrayContent(imageData),
						"media",
						"media.png"
					}
				},
				oauthParameters: null,
				accessToken: new TwitterToken(
					oauthToken: account.AccessTokenValue,
					oauthTokenSecret: account.AccessTokenSecret
				)
			);
			string responseContent;
			try {
				using (var client = httpClientFactory.CreateClient())
				using (var response = await client.SendAsync(message)) {
					responseContent = await response.Content.ReadAsStringAsync();
					if (!response.IsSuccessStatusCode) {
						logger.LogError("Twitter media upload failed. Status code: {StatusCode} Response: {Content}", response.StatusCode, responseContent);
						try {
							var errorResponse = JsonSnakeCaseSerializer.Deserialize<TwitterErrorResponse>(responseContent);
							if (errorResponse.Errors.Any(error => error.Code == 89)) {
								using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
									await db.DisassociateAuthServiceAccount(
										identityId: account.IdentityId
									);
								}
							}
						} catch {
							// swallow
						}
						return null;
					}
				}
				return JsonSnakeCaseSerializer.Deserialize<TwitterMediaUpload>(responseContent);
			} catch (Exception ex) {
				logger.LogError(ex, "HttpClient error during Twitter media upload.");
				return null;
			}
		}
		private async Task<( AuthServiceAccount account, AuthServiceAuthentication authentication, AuthenticationError? error )> VerifyRequestToken(
			string sessionId,
			AuthServiceRequestToken requestToken,
			string requestVerifier,
			UserAccountCreationAnalytics signUpAnalytics
		) {
			// request the access token from twitter
			var accessToken = await GetAccessToken(
				requestToken: requestToken.TokenValue,
				requestVerifier: requestVerifier
			);
			if (accessToken == null) {
				return (
					account: null,
					authentication: null,
					error: AuthenticationError.InvalidAuthToken
				);
			}
			// verify the credentials
			var user = await GetTwitterUser(accessToken);
			if (user == null) {
				return (
					account: null,
					authentication: null,
					error: AuthenticationError.InvalidAuthToken
				);
			}
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// look for an existing auth service account
				var authServiceAccount = await db.GetAuthServiceAccountByProviderUserId(AuthServiceProvider.Twitter, accessToken.UserId);
				if (authServiceAccount != null) {
					// update the user if necessary
					if (
						authServiceAccount.ProviderUserEmailAddress != user.Email ||
						authServiceAccount.ProviderUserName != user.Name ||
						authServiceAccount.ProviderUserHandle != accessToken.ScreenName
					) {
						// update the user
						authServiceAccount = await db.UpdateAuthServiceAccountUser(
							identityId: authServiceAccount.IdentityId,
							emailAddress: user.Email,
							isEmailAddressPrivate: false,
							name: user.Name,
							handle: accessToken.ScreenName
						);
					}
				} else {
					// create a new identity
					authServiceAccount = await db.CreateAuthServiceIdentity(
						provider: AuthServiceProvider.Twitter,
						providerUserId: accessToken.UserId,
						providerUserEmailAddress: user.Email,
						isEmailAddressPrivate: false,
						providerUserName: user.Name,
						providerUserHandle: accessToken.ScreenName,
						realUserRating: (
							user.Verified ?
								AuthServiceRealUserRating.Verified :
								AuthServiceRealUserRating.Unknown
						),
						signUpAnalytics: signUpAnalytics
					);
				}
				// create authentication
				var authentication = await db.CreateAuthServiceAuthentication(
					identityId: authServiceAccount.IdentityId,
					sessionId: sessionId
				);
				// store the access token
				await db.StoreAuthServiceAccessToken(
					identityId: authServiceAccount.IdentityId,
					requestId: requestToken.Id,
					tokenValue: accessToken.OAuthToken,
					tokenSecret: accessToken.OAuthTokenSecret
				);
				// return the auth service account
				return (
					account: authServiceAccount,
					authentication,
					error: null
				);
			}
		}
		public async Task<( AuthServiceAccount authServiceAccount, AuthServiceAuthentication authentication, UserAccount user, AuthenticationError? error )> Authenticate(
			string sessionId,
			string requestTokenValue,
			string requestVerifier,
			UserAccountCreationAnalytics signUpAnalytics
		) {
			// retrieve the request token
			AuthServiceRequestToken requestToken;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				requestToken = await db.GetAuthServiceRequestToken(
					tokenValue: requestTokenValue
				);
			}
			// attempt to parse sign up analytics from request token if parameter is null
			if (signUpAnalytics == null && requestToken.SignUpAnalytics != null) {
				try {
					signUpAnalytics = PostgresSerialization.DeserializeJson<UserAccountCreationAnalytics>(requestToken.SignUpAnalytics);
				} catch (Exception exception) {
					logger.LogError(exception, "Failed to deserialize sign up analytics from Twitter request token");
				}
			}
			// verify the request token
			var (authServiceAccount, authentication, error) = await VerifyRequestToken(
				sessionId: sessionId,
				requestToken: requestToken,
				requestVerifier: requestVerifier,
				signUpAnalytics: signUpAnalytics
			);
			if (error.HasValue) {
				return (
					authServiceAccount: null,
					authentication: null,
					user: null,
					error
				);
			}
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// check if the identity is associated with a user account
				if (authServiceAccount.AssociatedUserAccountId.HasValue) {
					return (
						authServiceAccount,
						authentication,
						user: await db.GetUserAccountById(
							authServiceAccount.AssociatedUserAccountId.Value
						),
						error: null
					);
				}
				// look for a user account with matching email address
				if (String.IsNullOrWhiteSpace(authServiceAccount.ProviderUserEmailAddress)) {
					return (
						authServiceAccount: null,
						authentication: null,
						user: null,
						error: AuthenticationError.EmailAddressRequired
					);
				}
				var userAccount = db.GetUserAccountByEmail(authServiceAccount.ProviderUserEmailAddress);
				if (userAccount != null) {
					// associate with auth service account
					authServiceAccount = await db.AssociateAuthServiceAccount(
						identityId: authServiceAccount.IdentityId,
						authenticationId: authentication.Id,
						userAccountId: userAccount.Id,
						associationMethod: AuthServiceAssociationMethod.Auto
					);
					userAccount.HasLinkedTwitterAccount = true;
					return (
						authServiceAccount,
						authentication,
						user: userAccount,
						error: null
					);
				}
				return (
					authServiceAccount,
					authentication,
					user: null,
					error: null
				);
			}
		}
		public async Task CancelRequest(
			string tokenValue
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				await db.CancelAuthServiceRequestToken(tokenValue);
			}
		}
		public async Task<TwitterRequestToken> GetBrowserAuthRequestToken(
			string redirectPath,
			UserAccountCreationAnalytics signUpAnalytics
		) {
			return await GetRequestToken(
				oauthCallback: authOptions.BrowserAuthCallback + QueryStringSerializer.Serialize(
					new[] {
						new KeyValuePair<string, string>("readup_redirect_path", redirectPath)
					},
					includePrefix: true
				),
				signUpAnalytics: signUpAnalytics
			);
		}
		public async Task<TwitterRequestToken> GetBrowserLinkRequestToken() {
			return await GetRequestToken(
				oauthCallback: authOptions.BrowserLinkCallback,
				signUpAnalytics: null
			);
		}
		public async Task<TwitterRequestToken> GetBrowserPopupRequestToken(string requestId) {
			return await GetRequestToken(
				oauthCallback: authOptions.BrowserPopupCallback + QueryStringSerializer.Serialize(
					new[] {
						new KeyValuePair<string, string>("readup_request_id", requestId)
					},
					includePrefix: true
				),
				signUpAnalytics: null
			);
		}
		public async Task<TwitterRequestToken> GetWebViewRequestToken() {
			return await GetRequestToken(
				oauthCallback: authOptions.WebViewCallback,
				signUpAnalytics: null
			);
		}
		public async Task<( AuthServiceAccount authServiceAccount, AuthenticationError? error )> LinkAccount(
			string sessionId,
			string requestTokenValue,
			string requestVerifier,
			long userAccountId
		) {
			// retrieve the request token
			AuthServiceRequestToken requestToken;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				requestToken = await db.GetAuthServiceRequestToken(
					tokenValue: requestTokenValue
				);
			}
			// verify the request token
			var (authServiceAccount, authentication, error) = await VerifyRequestToken(
				sessionId: sessionId,
				requestToken: requestToken,
				requestVerifier: requestVerifier,
				signUpAnalytics: null
			);
			// check if the identity is associated with a user account
			if (!authServiceAccount.AssociatedUserAccountId.HasValue) {
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					authServiceAccount = await db.AssociateAuthServiceAccount(
						identityId: authServiceAccount.IdentityId,
						authenticationId: authentication.Id,
						userAccountId: userAccountId,
						associationMethod: AuthServiceAssociationMethod.Link
					);
				}
			}
			return (
				authServiceAccount,
				error
			);
		}
		public async Task<string> GetAotdTweetText(
			Article article
		) {
			var targets = await AcquireBotTweetTargets(
				articleId: article.Id
			);
			var openingSegment = "🏆";
			if (targets.Length > 0) {
				openingSegment += " Congratulations " + targets;
			}
			return CreateTruncatedTweetText(
				segments: new[] {
					openingSegment,
					article.Title,
					"won Article of the Day!"
				},
				shrinkableSegmentIndex: 1,
				uri: routing.CreateCommentsUrl(article.Slug)
			);
		}
		public async Task TweetPostComment(
			Comment comment,
			UserAccount user
		) {
			await TweetFromLinkedAccounts(
				comment: comment,
				silentPost: null,
				articleSlug: comment.ArticleSlug,
				user: user
			);
		}
		public async Task TweetSilentPost(
			SilentPost silentPost,
			Article article,
			UserAccount user
		) {
			await TweetFromLinkedAccounts(
				comment: null,
				silentPost: silentPost,
				articleSlug: article.Slug,
				user: user
			);
		}
	}
}