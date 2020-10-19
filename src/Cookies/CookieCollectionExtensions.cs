using System;
using System.Linq;
using System.Text.RegularExpressions;
using api.Analytics;
using Microsoft.AspNetCore.Http;
using MyCookieOptions = api.Configuration.CookieOptions;

namespace api.Cookies {
	public static class CookieCollectionExtensions {
		private static readonly string authServiceBrowserPopupVariableName = "AuthServiceBrowserPopup";
		private static readonly string extensionInstallationRedirectPathCookieKey = "extensionInstallationRedirectPath";
		private static readonly string extensionVersionCookieKey = "extensionVersion";
		private static readonly string sessionIdCookieKey = "sessionId";
		private static readonly string variableCookieKeyPrefix = "_var";
		private static readonly char variableCookieKeySeparator = '.';
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
	}
}