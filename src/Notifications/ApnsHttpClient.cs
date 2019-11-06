using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace api.Notifications {
	public class ApnsHttpClient {
		private readonly HttpClient client;
		public ApnsHttpClient(HttpClient client) {
			this.client = client;
		}
		public async Task<IEnumerable<(string Token, ApnsResponse Response)>> Send(ILogger logger, params ApnsNotification[] notifications) {
			var errors = new List<(string Token, ApnsResponse Response)>();
			foreach (var notification in notifications) {
				var requestContent = JsonSerializer.Serialize(
					value: notification.Payload,
					options: new JsonSerializerOptions()
					{
						IgnoreNullValues = true,
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase
					}
				);
				foreach (var token in notification.Tokens) {
					var request = new HttpRequestMessage(
						method: HttpMethod.Post,
						requestUri: "/3/device/" + token
					)
					{
						Content = new StringContent(
							content: requestContent,
							encoding: Encoding.UTF8,
							mediaType: "application/json"
						),
						Version = new Version(2, 0),
					};
					if (notification.ReceiptId != null) {
						request.Headers.Add("apns-collapse-id", notification.ReceiptId);
					}
					var response = await client.SendAsync(request);
					if (!response.IsSuccessStatusCode) {
						ApnsResponse apnsResponse;
						Exception apnsResponseParseException;
						try {
							apnsResponse = JsonSerializer.Deserialize<ApnsResponse>(
								json: await response.Content.ReadAsStringAsync(),
								options: new JsonSerializerOptions()
								{
									AllowTrailingCommas = true,
									PropertyNameCaseInsensitive = true
								}
							);
							apnsResponseParseException = null;
						} catch (Exception ex) {
							apnsResponse = null;
							apnsResponseParseException = ex;
						}
						logger.LogError(
							"APNs dispatch failed. Status code: {StatusCode} Apns reason: {ApnsReason} Request content: {RequestContent}",
							response.StatusCode.ToString(),
							apnsResponse?.Reason,
							requestContent
						);
						if (apnsResponse != null) {
							errors.Add((token, apnsResponse));
						}
						if (apnsResponseParseException != null) {
							logger.LogError(apnsResponseParseException, "Failed to parse APNs response");
						}
					}
				}
			}
			return errors;
		}
	}
}