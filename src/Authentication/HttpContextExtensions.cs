using System.Security.Claims;
using System.Threading.Tasks;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace api.Authentication {
	public static class HttpContextExtensions {
		public static string GetSessionId(this HttpContext httpContext) {
			string sessionId;
			httpContext.Request.Cookies.TryGetValue("sessionId", out sessionId);
			return sessionId;
		}
	}
}