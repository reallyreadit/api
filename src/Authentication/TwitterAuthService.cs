using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Authentication {
	public class TwitterAuthService {
		private readonly IHttpClientFactory httpClientFactory;
		private readonly TwitterAuthOptions authOptions;
		private readonly DatabaseOptions databaseOptions;
		private readonly ILogger<AppleAuthService> logger;
		public TwitterAuthService(
			IHttpClientFactory httpClientFactory,
			IOptions<AuthenticationOptions> authOptions,
			IOptions<DatabaseOptions> databaseOptions,
			ILogger<AppleAuthService> logger
		) {
			this.httpClientFactory = httpClientFactory;
			this.authOptions = authOptions.Value.TwitterAuth;
			this.databaseOptions = databaseOptions.Value;
			this.logger = logger;
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
			TwitterAccessToken accessToken
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
		private async Task<TwitterAccessToken> GetAccessTokenAsync(
			string requestToken,
			string requestVerifier
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Post,
				uri: new Uri("https://api.twitter.com/oauth/access_token"),
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
		private async Task<TwitterRequestToken> GetRequestTokenAsync(
			string oauthCallback,
			UserAccountCreationAnalytics signUpAnalytics
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Post,
				uri: new Uri("https://api.twitter.com/oauth/request_token"),
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
		private async Task<TwitterUser> GetUserAsync(
			TwitterAccessToken accessToken
		) {
			var message = CreateRequestMessage(
				method: HttpMethod.Get,
				uri: new Uri("https://api.twitter.com/1.1/account/verify_credentials.json"),
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
			return JsonSerializer.Deserialize<TwitterUser>(
				responseContent,
				new JsonSerializerOptions() {
					AllowTrailingCommas = true,
					PropertyNameCaseInsensitive = true
				}
			);
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
		private async Task<( AuthServiceAccount account, AuthServiceAuthentication authentication, TwitterTokenAuthenticationError? error )> VerifyRequestTokenAsync(
			string sessionId,
			AuthServiceRequestToken requestToken,
			string requestVerifier,
			UserAccountCreationAnalytics signUpAnalytics
		) {
			// request the access token from twitter
			var accessToken = await GetAccessTokenAsync(
				requestToken: requestToken.TokenValue,
				requestVerifier: requestVerifier
			);
			if (accessToken == null) {
				return (
					account: null,
					authentication: null,
					error: TwitterTokenAuthenticationError.VerificationFailed
				);
			}
			// verify the credentials
			var user = await GetUserAsync(accessToken);
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
		public async Task<( AuthServiceAccount authServiceAccount, AuthServiceAuthentication authentication, UserAccount user, TwitterTokenAuthenticationError? error )> AuthenticateAsync(
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
			// TODO: parse sign up analytics from request token if parameter is null
			// verify the request token
			var (authServiceAccount, authentication, error) = await VerifyRequestTokenAsync(
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
						error: TwitterTokenAuthenticationError.EmailAddressRequired
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
		public async Task<TwitterRequestToken> GetBrowserRequestTokenAsync(
			string redirectPath,
			AuthServiceIntegration integrations,
			UserAccountCreationAnalytics signUpAnalytics
		) {
			var query = new List<KeyValuePair<string, string>>();
			if (!String.IsNullOrWhiteSpace(redirectPath)) {
				query.Add(
					new KeyValuePair<string, string>("readup_redirect_path", redirectPath)
				);
			}
			if (integrations != AuthServiceIntegration.None) {
				query.Add(
					new KeyValuePair<string, string>("readup_integrations", integrations.ToString())
				);
			}
			var callback = authOptions.BrowserCallback;
			if (query.Any()) {
				callback += '?' + String.Join(
					'&',
					query.Select(
						kvp => Uri.EscapeDataString(kvp.Key) + '=' + Uri.EscapeDataString(kvp.Value)
					)
				);
			}
			return await GetRequestTokenAsync(
				oauthCallback: callback,
				signUpAnalytics: signUpAnalytics
			);
		}
		public async Task<TwitterRequestToken> GetWebViewRequestTokenAsync() {
			return await GetRequestTokenAsync(
				oauthCallback: authOptions.WebViewCallback,
				signUpAnalytics: null
			);
		}
		public async Task<( AuthServiceAccount authServiceAccount, TwitterTokenAuthenticationError? error )> LinkAsync(
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
			var (authServiceAccount, authentication, error) = await VerifyRequestTokenAsync(
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
		public async Task<AuthServiceAccount> SetIntegrationPreferenceAsync(
			long identityId,
			AuthServiceIntegration integrations
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				return await db.SetAuthServiceAccountIntegrationPreference(
					identityId: identityId,
					isPostEnabled: integrations == AuthServiceIntegration.Post
				);
			}
		}
	}
}