using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyCookieOptions = api.Configuration.CookieOptions;

namespace api.Analytics {
	public static class ControllerExtension {
		private static readonly string extensionInstallationRedirectPathCookieKey = "extensionInstallationRedirectPath";
		private static readonly string extensionVersionCookieKey = "extensionVersion";
		private static readonly string sessionIdCookieKey = "sessionId";
		public static bool ClientVersionIsGreaterThanOrEqualTo(this Controller controller, IDictionary<ClientType, SemanticVersion> versions) {
			var client = controller.GetClientAnalytics();
			return (
				client != null &&
				versions.ContainsKey(client.Type) &&
				client.Version.CompareTo(versions[client.Type]) >= 0
			);
		}
		public static ClientAnalytics GetClientAnalytics(this Controller controller) {
			if (controller.Request.Headers.ContainsKey("X-Readup-Client")) {
				return ClientAnalytics.ParseClientString(controller.Request.Headers["X-Readup-Client"]);
			}
			return null;
		}
		public static string GetExtensionInstallationRedirectPathCookieValue(this Controller controller) {
			string redirectPath;
			controller.Request.Cookies.TryGetValue(extensionInstallationRedirectPathCookieKey, out redirectPath);
			return redirectPath;
		}
		public static SemanticVersion GetExtensionVersionCookieValue(this Controller controller) {
			string versionString;
			if (controller.Request.Cookies.TryGetValue(extensionVersionCookieKey, out versionString)) {
				try {
					return new SemanticVersion(versionString);
				} catch {
					return null;
				}
			}
			return null;
		}
		public static string GetSessionIdCookieValue(this Controller controller) {
			string sessionId;
			controller.Request.Cookies.TryGetValue(sessionIdCookieKey, out sessionId);
			return sessionId;
		}
		public static void SetExtensionVersionCookie(
			this Controller controller,
			SemanticVersion version,
			MyCookieOptions cookieOptions
		) {
			controller.Response.Cookies.Append(
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