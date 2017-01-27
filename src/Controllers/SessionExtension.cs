using System;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers {
	public static class SessionExtension {
		public static byte[] GetSessionKey(this Controller controller) {
			if (controller.Request.Cookies.ContainsKey("sessionKey")) {
				return Convert.FromBase64String(controller.Request.Cookies["sessionKey"]);
			}
			return null;
		}
	}
}