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
using api.Routing;
using api.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Authentication {
	public class TwitterAuthService {
		private readonly IHttpClientFactory httpClientFactory;
		private readonly TwitterAuthOptions authOptions;
		private readonly DatabaseOptions databaseOptions;
		private readonly ILogger<AppleAuthService> logger;
		private readonly RoutingService routing;
		private readonly IBackgroundTaskQueue taskQueue;
		public TwitterAuthService(
			IHttpClientFactory httpClientFactory,
			IOptions<AuthenticationOptions> authOptions,
			IOptions<DatabaseOptions> databaseOptions,
			ILogger<AppleAuthService> logger,
			RoutingService routing,
			IBackgroundTaskQueue taskQueue
		) {
			this.httpClientFactory = httpClientFactory;
			this.authOptions = authOptions.Value.TwitterAuth;
			this.databaseOptions = databaseOptions.Value;
			this.logger = logger;
			this.routing = routing;
			this.taskQueue = taskQueue;
		}
		private async Task<string> AcquireBotTweetTargets(
			long articleId
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// retrieve the source
				var source = await db.GetSourceOfArticle(articleId);
				// search for twitter handle if we haven't already
				if (source.TwitterHandleAssignment == TwitterHandleAssignment.None) {
					var searchResults = await SearchTwitterUsers(source.Name);
					source = await db.AssignTwitterHandleToSource(
						sourceId: source.Id,
						twitterHandle: searchResults.FirstOrDefault()?.ScreenName,
						twitterHandleAssignment: TwitterHandleAssignment.NameSearch
					);
				}
				// retrieve the authors
				var authors = (await db.GetAuthorsOfArticle(articleId)).ToArray();
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
						authors[i] = await db.AssignTwitterHandleToAuthor(
							authorId: author.Id,
							twitterHandle: searchResults.FirstOrDefault()?.ScreenName,
							twitterHandleAssignment: searchMethod
						);
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
			IEnumerable<KeyValuePair<string, string>> bodyParameters,
			IDictionary<string, string> oauthParameters,
			ITwitterToken accessToken
		) {
			// check for null arguments
			if (queryStringParameters == null) {
				queryStringParameters = new KeyValuePair<string, string>[0];
			}
			if (bodyParameters == null) {
				bodyParameters = new KeyValuePair<string, string>[0];
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
					.Concat(bodyParameters)
					.Concat(oauthParameters)
					.Concat(standardOauthParameters),
				consumerSecret: authOptions.ConsumerSecret,
				accessTokenSecret: accessToken?.OAuthTokenSecret
			);
			
			// create the message
			var message = new HttpRequestMessage(method, uri);
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
			if (bodyParameters.Any()) {
				message.Content = new FormUrlEncodedContent(bodyParameters);
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
				bodyParameters: null,
				oauthParameters: new Dictionary<string, string>() {
					{ "oauth_token", requestToken },
					{ "oauth_verifier", requestVerifier }
				},
				accessToken: null
			);
			string responseContent;
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
		}
		private async Task<TwitterRequestToken> GetRequestToken(
			string oauthCallback,
			UserAccountCreationAnalytics signUpAnalytics
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Post,
				uri: new Uri(authOptions.TwitterApiServerUrl + "/oauth/request_token"),
				queryStringParameters: null,
				bodyParameters: null,
				oauthParameters: new Dictionary<string, string>() {
					{ "oauth_callback", oauthCallback }
				},
				accessToken: null
			);
			string responseContent;
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
				bodyParameters: null,
				oauthParameters: null,
				accessToken: accessToken
			);
			string responseContent;
			using (var client = httpClientFactory.CreateClient())
			using (var response = await client.SendAsync(message)) {
				responseContent = await response.Content.ReadAsStringAsync();
				if (!response.IsSuccessStatusCode) {
					logger.LogError("Twitter credentials verification failed. Status code: {StatusCode} Response: {Content}", response.StatusCode, responseContent);
					return null;
				}
			}
			return JsonSnakeCaseSerializer.Deserialize<TwitterUser>(responseContent);
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
				bodyParameters: null,
				oauthParameters: null,
				accessToken: authOptions.SearchAccount
			);
			string responseContent;
			using (var client = httpClientFactory.CreateClient())
			using (var response = await client.SendAsync(message)) {
				responseContent = await response.Content.ReadAsStringAsync();
				if (!response.IsSuccessStatusCode) {
					logger.LogError("Twitter user search failed. Status code: {StatusCode} Response: {Content}", response.StatusCode, responseContent);
					return null;
				}
			}
			return JsonSnakeCaseSerializer.Deserialize<TwitterUserSearchResult[]>(responseContent);
		}
		private async Task TweetFromLinkedAccounts(
			string status,
			long? commentId,
			long? silentPostId,
			long userAccountId
		) {
			IEnumerable<AuthServiceAccount> twitterAccounts;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				twitterAccounts = (
						await db.GetAuthServiceAccountsForUserAccount(
							userAccountId: userAccountId
						)
					)
					.Where(
						account => account.Provider == AuthServiceProvider.Twitter
					)
					.ToArray();
			}
			if (twitterAccounts.Any()) {
				taskQueue.QueueBackgroundWorkItem(
					async cancellationToken => {
						var tweets = new List<( long IdentityId, TwitterTweet Tweet )>();
						foreach (var account in twitterAccounts) {
							var tweet = await Tweet(
								status: status,
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
										commentId: commentId,
										silentPostId: silentPostId,
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
			AuthServiceAccount account
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Post,
				uri: new Uri(authOptions.TwitterApiServerUrl + "/1.1/statuses/update.json"),
				queryStringParameters: new Dictionary<string, string>() {
					{ "status", status }
				},
				bodyParameters: null,
				oauthParameters: null,
				accessToken: new TwitterToken(
					oauthToken: account.AccessTokenValue,
					oauthTokenSecret: account.AccessTokenSecret
				)
			);
			string responseContent;
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
					signUpAnalytics = PostgresJsonSerialization.Deserialize<UserAccountCreationAnalytics>(requestToken.SignUpAnalytics);
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
			Comment comment
		) {
			await TweetFromLinkedAccounts(
				status: CreateTruncatedTweetText(
					segments: new [] {
						CommentingService.RenderCommentTextToPlainText(comment.Text)
					},
					shrinkableSegmentIndex: 0,
					uri: routing.CreateCommentUrl(comment.ArticleSlug, comment.Id)
				),
				commentId: comment.Id,
				silentPostId: null,
				userAccountId: comment.UserAccountId
			);
		}
		public async Task TweetSilentPost(
			SilentPost silentPost,
			string articleSlug
		) {
			await TweetFromLinkedAccounts(
				status: routing.CreateCommentsUrl(articleSlug).ToString(),
				commentId: null,
				silentPostId: silentPost.Id,
				userAccountId: silentPost.UserAccountId
			);
		}
	}
}