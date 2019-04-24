using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace api.Versioning {
	public static class ControllerExtension {
		private static IDictionary<string, ClientType> clientTypeMap = new Dictionary<string, ClientType>() {
			{ "ios/app", ClientType.IosApp },
			{ "ios/extension", ClientType.IosExtension },
			{ "web/app/client", ClientType.WebAppClient },
			{ "web/app/server", ClientType.WebAppServer },
			{ "web/extension", ClientType.WebExtension }
		};
		public static bool ClientVersionIsGreaterThanOrEqualTo(this Controller controller, IDictionary<ClientType, SemanticVersion> versions) {
			var clientInfo = controller.GetClientInfo();
			return (
				clientInfo != null &&
				versions.ContainsKey(clientInfo.Type) &&
				clientInfo.Version.CompareTo(versions[clientInfo.Type]) >= 0
			);
		}
		public static ClientInfo GetClientInfo(this Controller controller) {
			if (controller.Request.Headers.ContainsKey("X-Readup-Client")) {
				var match = Regex.Match(
					input: controller.Request.Headers["X-Readup-Client"],
					pattern: @"([a-z/]+)(#\w+)?@(\d+\.\d+\.\d+)$"
				);
				if (match.Success && clientTypeMap.ContainsKey(match.Groups[1].Value)) {
					return new ClientInfo(
						type: clientTypeMap[match.Groups[1].Value],
						version: new SemanticVersion(match.Groups[3].Value),
						mode: match.Groups[2].Value.TrimStart('#')
					);
				}
			}
			return null;
		}
	}
}