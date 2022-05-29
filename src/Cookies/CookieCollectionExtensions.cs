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
using System.Linq;
using System.Text.RegularExpressions;
using api.Analytics;
using api.Encryption;
using Microsoft.AspNetCore.Http;
using MyCookieOptions = api.Configuration.CookieOptions;
using TokenizationOptions = api.Configuration.TokenizationOptions;

namespace api.Cookies {
	public static class CookieCollectionExtensions {
		private static readonly string authServiceBrowserPopupVariableName = "AuthServiceBrowserPopup";
		private static readonly string extensionInstallationRedirectPathCookieKey = "extensionInstallationRedirectPath";
		private static readonly string extensionVersionCookieKey = "extensionVersion";
		private static readonly string provisionalSessionKeyCookieKey = "provisionalSessionKey";
		private static readonly string sessionIdCookieKey = "sessionId";
		private static readonly string variableCookieKeyPrefix = "_var";
		private static readonly char variableCookieKeySeparator = '.';
		private static CookieOptions GetProvisionalSessionKeyCookieOptions(
			MyCookieOptions cookieOptions
		) => new CookieOptions() {
			Domain = cookieOptions.Domain,
			HttpOnly = true,
			MaxAge = null,
			Path = "/",
			SameSite = SameSiteMode.None,
			Secure = true
		};
		public static void ClearProvisionalSessionKeyCookie(
			this IResponseCookies cookies,
			MyCookieOptions cookieOptions
		) {
			cookies.Delete(
				key: provisionalSessionKeyCookieKey,
				options: GetProvisionalSessionKeyCookieOptions(cookieOptions)
			);
		}
		public static string GetAuthServiceBrowserPopupCookieValue(
			this IRequestCookieCollection cookies,
			string requestId
		) {
			var keyRegex = new Regex(
				String.Join(
					separator: @"\" + variableCookieKeySeparator,
					"^" + variableCookieKeyPrefix,
					authServiceBrowserPopupVariableName,
					@"\d+",
					requestId.Replace("-", @"\-") + "$"
				)
			);
			var matchingKey = cookies.Keys.SingleOrDefault(keyRegex.IsMatch);
			if (matchingKey == null) {
				return null;
			}
			string value;
			cookies.TryGetValue(matchingKey, out value);
			return value;
		}
		public static string GetExtensionInstallationRedirectPathCookieValue(
			this IRequestCookieCollection cookies
		) {
			string redirectPath;
			cookies.TryGetValue(extensionInstallationRedirectPathCookieKey, out redirectPath);
			return redirectPath;
		}
		public static SemanticVersion GetExtensionVersionCookieValue(
			this IRequestCookieCollection cookies
		) {
			string versionString;
			if (
				cookies.TryGetValue(extensionVersionCookieKey, out versionString)
			) {
				try {
					return new SemanticVersion(versionString);
				} catch {
					return null;
				}
			}
			return null;
		}
		public static long? GetProvisionalSessionKeyCookieValue(
			this IRequestCookieCollection cookies,
			TokenizationOptions tokenizationOptions
		) {
			string provisionalSessionKey;
			if (
				cookies.TryGetValue(provisionalSessionKeyCookieKey, out provisionalSessionKey)
			) {
				return Int64.Parse(
					StringEncryption.Decrypt(
						text: UrlSafeBase64.Decode(
							provisionalSessionKey
						),
						key: tokenizationOptions.EncryptionKey
					)
				);
			}
			return null;
		}
		public static string GetSessionIdCookieValue(
			this IRequestCookieCollection cookies
		) {
			string sessionId;
			cookies.TryGetValue(sessionIdCookieKey, out sessionId);
			return sessionId;
		}
		public static void SetAuthServiceBrowserPopupCookie(
			this IResponseCookies cookies,
			string requestId,
			string value,
			string apiServerHost
		) {
			cookies.Append(
				key: String.Join(
					separator: variableCookieKeySeparator,
					variableCookieKeyPrefix,
					authServiceBrowserPopupVariableName,
					DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
					requestId
				),
				value: value,
				options: new CookieOptions() {
					Domain = apiServerHost,
					HttpOnly = true,
					MaxAge = TimeSpan.FromMinutes(5),
					Path = "/Auth",
					SameSite = SameSiteMode.None,
					Secure = true
				}
			);
		}
		public static void SetExtensionVersionCookie(
			this IResponseCookies cookies,
			SemanticVersion version,
			MyCookieOptions cookieOptions
		) {
			cookies.Append(
				key: extensionVersionCookieKey,
				value: version.ToString(),
				options: new CookieOptions() {
					Domain = cookieOptions.Domain,
					HttpOnly = false,
					MaxAge = TimeSpan.FromDays(365),
					Path = "/",
					SameSite = SameSiteMode.None,
					Secure = true
				}
			);
		}
		public static void SetProvisionalSessionKeyCookie(
			this IResponseCookies cookies,
			long id,
			TokenizationOptions tokenizationOptions,
			MyCookieOptions cookieOptions
		) {
			cookies.Append(
				key: provisionalSessionKeyCookieKey,
				value: UrlSafeBase64.Encode(
					StringEncryption.Encrypt(
						text: id.ToString(),
						key: tokenizationOptions.EncryptionKey
					)
				),
				options: GetProvisionalSessionKeyCookieOptions(cookieOptions)
			);
		}
		public static void SetSessionIdCookie(
			this IResponseCookies cookies,
			MyCookieOptions cookieOptions
		) {
			var bytes = new byte[8];
			var random = new Random();
			random.NextBytes(bytes);
			cookies.Append(
				key: sessionIdCookieKey,
				value: String.Concat(
					bytes.Select(
						b => b.ToString("x2")
					)
				),
				options: new CookieOptions() {
					Domain = cookieOptions.Domain,
					HttpOnly = true,
					MaxAge = null,
					Path = "/",
					SameSite = SameSiteMode.None,
					Secure = true
				}
			);
		}
	}
}