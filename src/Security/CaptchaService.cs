using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace api.Security {
	public class CaptchaService {
		private readonly CaptchaOptions authOpts;
		public CaptchaService(IOptions<CaptchaOptions> authOpts) {
			this.authOpts = authOpts.Value;
		}
		public async Task<CaptchaVerificationResponse> Verify(string response) {
			if (!authOpts.VerifyCaptcha) {
				return null;
			}
			var httpResponse = await Program.HttpClient.PostAsync(
				requestUri: QueryHelpers.AddQueryString(
					uri: "https://www.google.com/recaptcha/api/siteverify",
					queryString: new Dictionary<string, string>() {
						{ "secret", "6Lejf38UAAAAAO6UqD6hMFQuArUBx7NCLJoaoG3P" },
						{ "response", response }
					}
				),
				content: null
			);
			if (!httpResponse.IsSuccessStatusCode) {
				return new CaptchaVerificationResponse();
			}
			return JsonConvert.DeserializeObject<CaptchaVerificationResponse>(
				await httpResponse.Content.ReadAsStringAsync()
			);
		}
	}
}