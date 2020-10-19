using System;
using api.Analytics;
using Microsoft.AspNetCore.Http;
using MyCookieOptions = api.Configuration.CookieOptions;

namespace api.Cookies {
	public static class CookieCollectionExtensions {
		private static readonly string extensionInstallationRedirectPathCookieKey = "extensionInstallationRedirectPath";
		private static readonly string extensionVersionCookieKey = "extensionVersion";
		private static readonly string sessionIdCookieKey = "sessionId";
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