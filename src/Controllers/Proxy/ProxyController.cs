// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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