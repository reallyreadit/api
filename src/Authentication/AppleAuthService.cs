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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace api.Authentication {
	public class AppleAuthService {
		private static IEnumerable<AppleJwk> appleJwks = new AppleJwk[0];
		private readonly SigningCredentials clientSecretSigningCredentials;
		private readonly IHttpClientFactory httpClientFactory;
		private readonly AppleAuthOptions authOptions;
		private readonly DatabaseOptions databaseOptions;
		private readonly ILogger<AppleAuthService> logger;
		public AppleAuthService(
			SigningCredentials clientSecretSigningCredentials,
			IHttpClientFactory httpClientFactory,
			IOptions<AuthenticationOptions> authOptions,
			IOptions<DatabaseOptions> databaseOptions,
			ILogger<AppleAuthService> logger
		) {
			this.clientSecretSigningCredentials = clientSecretSigningCredentials;
			this.httpClientFactory = httpClientFactory;
			this.authOptions = authOptions.Value.AppleAuth;
			this.databaseOptions = databaseOptions.Value;
			this.logger = logger;
		}
		private JwtSecurityToken CreateClientSecret(string sub) {
			var header = new JwtHeader(clientSecretSigningCredentials);
			header.Add(JwtHeaderParameterNames.Kid, authOptions.ClientSecretSigningKeyId);
			header.Remove(JwtHeaderParameterNames.Typ);
			var now = DateTime.UtcNow;
			var payload = new JwtPayload(
				issuer: authOptions.DeveloperTeamId,
				audience: authOptions.ClientSecretAudience,
				claims: new[] {
					new Claim(JwtRegisteredClaimNames.Sub, sub)
				},
				notBefore: null,
				expires: now.AddMonths(5),
				issuedAt: now
			);
			return new JwtSecurityToken(header, payload);
		}
		private async Task<AppleJwk> GetAppleJwk(string kid) {
			if (!appleJwks.Any(key => key.Kid == kid)) {
				using (var client = httpClientFactory.CreateClient())
				using (var response = await client.GetAsync(authOptions.AppleJwkUrl)) {
					var keySet = JsonSerializer.Deserialize<AppleJwkSet>(
						await response.Content.ReadAsStringAsync(),
						new JsonSerializerOptions() {
							AllowTrailingCommas = true,
							PropertyNameCaseInsensitive = true
						}
					);
					appleJwks = keySet.Keys;
				}
			}
			return appleJwks.FirstOrDefault(key => key.Kid == kid);
		}
		private async Task<(AuthServiceAccount account, AuthServiceAuthentication authentication, string emailAddress, AuthenticationError? error)> VerifyCredentials(
			string sessionId,
			string rawIdToken,
			string authCode,
			string emailAddress,
			AppleRealUserRating? appleRealUserRating,
			UserAccountCreationAnalytics signUpAnalytics,
			AppleClient client
		) {
			// check the session id
			if (String.IsNullOrWhiteSpace(sessionId)) {
				return (
					account: null,
					authentication: null,
					emailAddress: null,
					error: AuthenticationError.InvalidSessionId
				);
			}
			// parse the id token and verify with apple
			var idToken = new JwtSecurityToken(rawIdToken);
			var tokenResponse = await VerifyIdToken(idToken, authCode, client);
			if (tokenResponse == null) {
				return (
					account: null,
					authentication: null,
					emailAddress: null,
					error: AuthenticationError.InvalidAuthToken
				);
			}
			// resolve the provider user id
			var providerUserId = idToken.Subject;
			// resolve the provider user email address
			var providerUserEmail = emailAddress ?? (string)idToken.Payload["email"];
			object payloadIsPrivateEmailValue;
			var isProviderUserEmailPrivate = (
				idToken.Payload.TryGetValue("is_private_email", out payloadIsPrivateEmailValue) &&
				(payloadIsPrivateEmailValue as string) == "true"
			);
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// look for an existing auth service account
				var authServiceAccount = await db.GetAuthServiceAccountByProviderUserId(AuthServiceProvider.Apple, providerUserId);
				if (authServiceAccount != null) {
					// update the email address if necessary
					if (authServiceAccount.ProviderUserEmailAddress != providerUserEmail) {
						// update email address
						authServiceAccount = await db.UpdateAuthServiceAccountUser(
							identityId: authServiceAccount.IdentityId,
							emailAddress: providerUserEmail,
							isEmailAddressPrivate: isProviderUserEmailPrivate,
							name: null,
							handle: null
						);
					}
				} else {
					// create a new identity
					AuthServiceRealUserRating? realUserRating;
					switch (appleRealUserRating) {
						case AppleRealUserRating.LikelyReal:
							realUserRating = AuthServiceRealUserRating.LikelyReal;
							break;
						case AppleRealUserRating.Unknown:
							realUserRating = AuthServiceRealUserRating.Unknown;
							break;
						case AppleRealUserRating.Unsupported:
							realUserRating = AuthServiceRealUserRating.Unsupported;
							break;
						default:
							realUserRating = null;
							break;
					}
					authServiceAccount = await db.CreateAuthServiceIdentity(
						provider: AuthServiceProvider.Apple,
						providerUserId: providerUserId,
						providerUserEmailAddress: providerUserEmail,
						isEmailAddressPrivate: isProviderUserEmailPrivate,
						providerUserName: null,
						providerUserHandle: null,
						realUserRating: realUserRating,
						signUpAnalytics: signUpAnalytics
					);
				}
				// create authentication
				var authentication = await db.CreateAuthServiceAuthentication(
					identityId: authServiceAccount.IdentityId,
					sessionId: sessionId
				);
				// store the refresh token
				await db.CreateAuthServiceRefreshToken(
					identityId: authServiceAccount.IdentityId,
					rawValue: tokenResponse.RefreshToken
				);
				// return the auth service account
				return (
					account: authServiceAccount,
					authentication: authentication,
					emailAddress: providerUserEmail,
					error: null
				);
			}
		}
		private async Task<AppleTokenResponse> VerifyIdToken(JwtSecurityToken idToken, string authCode, AppleClient client) {
			var tokenHandler = new JwtSecurityTokenHandler();
			SecurityToken validatedIdToken;
			string appOrServiceId;
			switch (client) {
				case AppleClient.Ios:
					appOrServiceId = authOptions.DeveloperAppId;
					break;
				case AppleClient.Web:
					appOrServiceId = authOptions.DeveloperWebServiceId;
					break;
				default:
					throw new ArgumentException("Unexpected AppleClient");
			}
			try {
				tokenHandler.ValidateToken(
					idToken.RawData,
					new TokenValidationParameters() {
						IssuerSigningKey = new JsonWebKey(
							JsonSerializer.Serialize(
								await GetAppleJwk(idToken.Header.Kid)
							)
						),
						ValidAudience = appOrServiceId,
						ValidIssuer = authOptions.IdTokenIssuer
					},
					out validatedIdToken
				);
			} catch (Exception ex) {
				logger.LogError(ex, "Exception thrown during Apple ID token validation. Token value: {TokenValue}", idToken.RawData);
				return null;
			}
			if (validatedIdToken != null) {
				var requestFormValues = new Dictionary<string, string>() {
					{ "client_id", appOrServiceId },
					{ "client_secret", tokenHandler.WriteToken(CreateClientSecret(appOrServiceId)) },
					{ "code", authCode },
					{ "grant_type", "authorization_code" }
				};
				if (client == AppleClient.Web) {
					requestFormValues.Add("redirect_uri", authOptions.WebAuthRedirectUrl);
				}
				try {
					using (var httpClient = httpClientFactory.CreateClient())
					using (
						var response = await httpClient.PostAsync(
							authOptions.IdTokenValidationUrl,
							new FormUrlEncodedContent(requestFormValues)
						)
					) {
						var content = await response.Content.ReadAsStringAsync();
						if (response.IsSuccessStatusCode) {
							var tokenResponse = JsonSnakeCaseSerializer.Deserialize<AppleTokenResponse>(content);
							return tokenResponse;
						} else {
							var errorResponse = JsonSnakeCaseSerializer.Deserialize<AppleErrorResponse>(content);
							logger.LogError(
								"Error verifying Apple ID token. Error: {Error} Token value: {TokenValue}",
								errorResponse.Error,
								idToken.RawData
							);
						}
					}
				} catch (Exception ex) {
					logger.LogError(ex, "HttpClient error during Apple ID token verification.");
				}
			} else {
				logger.LogError("Validated Apple ID token is null. Token value: {TokenValue}", idToken.RawData);
			}
			return null;
		}
		public async Task<( long? authenticationId, UserAccount user, AuthenticationError? error )> AuthenticateAppleIdCredential(
			string sessionId,
			string rawIdToken,
			string authCode,
			string emailAddress,
			AppleRealUserRating? appleRealUserRating,
			UserAccountCreationAnalytics signUpAnalytics,
			AppleClient client
		) {
			// verify the credentials
			var (authServiceAccount, authentication, providerUserEmail, error) = await VerifyCredentials(
				sessionId: sessionId,
				rawIdToken: rawIdToken,
				authCode: authCode,
				emailAddress: emailAddress,
				appleRealUserRating: appleRealUserRating,
				signUpAnalytics: signUpAnalytics,
				client: client
			);
			if (error.HasValue) {
				return (
					authenticationId: null,
					user: null,
					error
				);
			}
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// check if the identity is associated with a user account
				if (authServiceAccount.AssociatedUserAccountId.HasValue) {
					return (
						authenticationId: null,
						user: await db.GetUserAccountById(
							authServiceAccount.AssociatedUserAccountId.Value
						),
						error: null
					);
				}
				// look for a user account with matching email address
				var userAccount = db.GetUserAccountByEmail(providerUserEmail);
				if (userAccount != null) {
					// associate with auth service account
					authServiceAccount = await db.AssociateAuthServiceAccount(
						identityId: authServiceAccount.IdentityId,
						authenticationId: authentication.Id,
						userAccountId: userAccount.Id,
						associationMethod: AuthServiceAssociationMethod.Auto
					);
					return (
						authenticationId: null,
						user: userAccount,
						error: null
					);
				}
				return (
					authenticationId: authentication.Id,
					user: null,
					error: null
				);
			}
		}
		public async Task<(AuthServiceAccount authServiceAccount, AuthenticationError? error )> LinkAccount(
			string sessionId,
			string rawIdToken,
			string authCode,
			string emailAddress,
			AppleRealUserRating? appleRealUserRating,
			UserAccountCreationAnalytics signUpAnalytics,
			AppleClient client,
			long userAccountId
		) {
			// verify the credentials
			var (authServiceAccount, authentication, providerUserEmail, error) = await VerifyCredentials(
				sessionId: sessionId,
				rawIdToken: rawIdToken,
				authCode: authCode,
				emailAddress: emailAddress,
				appleRealUserRating: appleRealUserRating,
				signUpAnalytics: signUpAnalytics,
				client: client
			);
			if (error.HasValue) {
				return (
					authServiceAccount: null,
					error
				);
			}
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
	}
}