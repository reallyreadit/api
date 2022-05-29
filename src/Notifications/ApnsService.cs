// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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
		private readonly string[] ApnsUnregisterErrorReasons = new [] { "BadDeviceToken", "Unregistered" };
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
							try {
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
							} catch (Exception ex) {
								logger.LogError(ex, "HttpClient error during APNs dispatch.");
							}
						}
					}
					if (errors.Any(error => ApnsUnregisterErrorReasons.Contains(error.Response.Reason))) {
						using (var db = new NpgsqlConnection(dbOptions.ConnectionString)) {
							foreach (
								var error in errors.Where(
									error => ApnsUnregisterErrorReasons.Contains(error.Response.Reason)
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