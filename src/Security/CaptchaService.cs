using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace api.Security {
	public class CaptchaService {
		private readonly CaptchaOptions authOpts;
		private readonly IHttpClientFactory httpClientFactory;
		private readonly ILogger<CaptchaService> logger;
		public CaptchaService(
			IOptions<CaptchaOptions> authOpts,
			IHttpClientFactory httpClientFactory,
			ILogger<CaptchaService> logger
		) {
			this.authOpts = authOpts.Value;
			this.httpClientFactory = httpClientFactory;
			this.logger = logger;
		}
		public async Task<CaptchaVerificationResponse> Verify(string response) {
			if (!authOpts.VerifyCaptcha) {
				return null;
			}
			try {
				var httpResponse = await httpClientFactory.CreateClient().PostAsync(
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
			} catch (Exception ex) {
				logger.LogError(ex, "HttpClient error during Captcha verification.");
				return new CaptchaVerificationResponse();
			}
		}
	}
}