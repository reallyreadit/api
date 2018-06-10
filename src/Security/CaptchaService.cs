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
		public async Task<bool> IsValid(string secret, string response) {
			if (!authOpts.VerifyCaptcha) {
				return true;
			}
			var httpResponse = await Program.HttpClient.PostAsync(
				requestUri: QueryHelpers.AddQueryString(
					uri: $"https://www.google.com/recaptcha/api/siteverify",
					queryString: new Dictionary<string, string>() {
						{ "secret", secret },
						{ "response", response }
					}
				),
				content: null
			);
			if (!httpResponse.IsSuccessStatusCode) {
				return false;
			}
			var verificationResponse = JsonConvert.DeserializeObject<CaptchaVerificationResponse>(
				await httpResponse.Content.ReadAsStringAsync()
			);
			return verificationResponse.Success;
		}
	}
}