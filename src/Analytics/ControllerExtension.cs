using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace api.Analytics {
	public static class ControllerExtension {
		public static bool ClientVersionIsGreaterThanOrEqualTo(this Controller controller, IDictionary<ClientType, SemanticVersion> versions) {
			var client = controller.GetRequestAnalytics().Client;
			return (
				client != null &&
				versions.ContainsKey(client.Type) &&
				client.Version.CompareTo(versions[client.Type]) >= 0
			);
		}
		public static RequestAnalytics GetRequestAnalytics(this Controller controller) {
			if (controller.Request.Headers.ContainsKey("X-Readup-Client")) {
				var match = Regex.Match(
					input: controller.Request.Headers["X-Readup-Client"],
					pattern: @"([a-z\-/]+)(#\w+)?@(\d+\.\d+\.\d+)$"
				);
				if (match.Success && ClientTypeDictionary.StringToEnum.ContainsKey(match.Groups[1].Value)) {
					return new RequestAnalytics(
						client: new ClientAnalytics(
							type: ClientTypeDictionary.StringToEnum[match.Groups[1].Value],
							version: new SemanticVersion(match.Groups[3].Value),
							mode: match.Groups[2].Success ?
								match.Groups[2].Value.TrimStart('#') :
								null
						)
					);
				}
			}
			return null;
		}
	}
}