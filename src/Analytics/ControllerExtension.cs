using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace api.Analytics {
	public static class ControllerExtension {
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
	}
}