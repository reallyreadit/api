using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Notifications {
	public class ApnsService {
		private readonly string ApnsUnregisteredErrorString = "Unregistered";
		private readonly HttpClient client;
		private readonly DatabaseOptions dbOptions;
		private readonly ILogger<ApnsService> logger;
		public ApnsService(
			HttpClient client,
			IOptions<DatabaseOptions> dbOptions,
			ILogger<ApnsService> logger
		) {
			client.DefaultRequestVersion = new Version(2, 0);
			this.client = client;
			this.dbOptions = dbOptions.Value;
			this.logger = logger;
		}
		public async Task Send(params ApnsNotification[] notifications) {
			var errors = new List<( string Token, ApnsResponse Response )>();
			foreach (var notification in notifications) {
				var requestContent = JsonSerializer.Serialize(
					value: notification.Payload,
					options: new JsonSerializerOptions() {
						IgnoreNullValues = true,
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase
					}
				);
				foreach (var token in notification.Tokens) {
					var response = await client.PostAsync(
						requestUri: "/3/device/" + token,
						content: new StringContent(
							content: requestContent,
							encoding: Encoding.UTF8,
							mediaType: "application/json"
						)
					);
					if (!response.IsSuccessStatusCode) {
						ApnsResponse apnsResponse;
						Exception apnsResponseParseException;
						try {
							apnsResponse = JsonSerializer.Deserialize<ApnsResponse>(
								json: await response.Content.ReadAsStringAsync(),
								options: new JsonSerializerOptions() {
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
			if (errors.Any(error => error.Response.Reason == ApnsUnregisteredErrorString)) {
				using (var db = new NpgsqlConnection(dbOptions.ConnectionString)) {
					foreach (
						var error in errors.Where(
							error => error.Response.Reason == ApnsUnregisteredErrorString
						)
					) {
						await db.UnregisterNotificationPushDeviceByToken(
							token: error.Token,
							reason: NotificationPushUnregistrationReason.ServiceUnregistered
						);
					}
				}
			}
		}
	}
}