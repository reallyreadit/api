using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Proxy {
	public class ProxyController : Controller {
		public async Task Article(
			[FromServices] IHttpClientFactory httpClientFactory,
			[FromQuery] string url
		) {
			using (var httpClient = httpClientFactory.CreateClient()) {
				httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_13_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
				using (var response = await httpClient.GetAsync(url)) {
					Response.StatusCode = (int)response.StatusCode;
					Response.ContentType = response.Content.Headers.ContentType.MediaType;
					await response.Content.CopyToAsync(Response.Body);
				}
			}
		}
	}
}