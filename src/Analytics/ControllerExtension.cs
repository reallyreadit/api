using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace api.Analytics {
	public static class ControllerExtension {
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
		public static string GetSessionIdCookieValue(this Controller controller) {
			string sessionId;
			controller.Request.Cookies.TryGetValue(sessionIdCookieKey, out sessionId);
			return sessionId;
		}
	}
}