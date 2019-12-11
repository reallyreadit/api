using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using api.BackgroundProcessing;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using api.Encryption;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Notifications {
	public class ApnsService {
		private readonly string ApnsUnregisteredErrorString = "Unregistered";
		private readonly HttpClient client;
		private readonly DatabaseOptions dbOptions;
		private readonly ILogger<ApnsService> logger;
		private readonly ObfuscationService obfuscation;
		private readonly IBackgroundTaskQueue taskQueue;
		public ApnsService(
			HttpClient client,
			IOptions<DatabaseOptions> dbOptions,
			IOptions<PushNotificationsOptions> pushOptions,
			ObfuscationService obfuscation,
			ILogger<ApnsService> logger,
			IBackgroundTaskQueue taskQueue
		) {
			client.BaseAddress = new Uri(
				uriString: pushOptions.Value.ApnsServer.CreateUrl()
			);
			client.DefaultRequestHeaders.Add("apns-push-type", "alert");
			client.DefaultRequestHeaders.Add("apns-topic", pushOptions.Value.ApnsTopic);
			client.DefaultRequestVersion = new Version(2, 0);
			this.client = client;
			this.dbOptions = dbOptions.Value;
			this.logger = logger;
			this.obfuscation = obfuscation;
			this.taskQueue = taskQueue;
		}
		public void Send(params ApnsNotification[] notifications) {
			taskQueue.QueueBackgroundWorkItem(
				async cancellationToken => {
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
							if (notification.ReceiptId.HasValue) {
								request.Headers.Add("apns-collapse-id", obfuscation.Encode(notification.ReceiptId.Value));
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
								// temporarily log all messages sent to jeff's devices to debug badge count issue
							} else if (
								token == "8d2ee02a00de0321b2305db98ca9e6735e46757bcd13b3194405ebd37f5cfc4b" ||
								token == "2d993aefe7045cba0d3cdab8df62b58047cf9947298b0b1bd991ff7be22108f9"
							) {
								logger.LogError(
									"APNs dispatch to Jeff's device with token: {Token} With badge count: {BadgeCount}",
									token,
									notification.Payload.Aps.Badge.ToString()
								);
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
			);
		}
	}
}