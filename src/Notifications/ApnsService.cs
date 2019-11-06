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
		private readonly ApnsHttpClient client;
		private readonly DatabaseOptions dbOptions;
		private readonly ILogger<ApnsService> logger;
		private readonly ObfuscationService obfuscation;
		private readonly IBackgroundTaskQueue taskQueue;
		public ApnsService(
			ApnsHttpClient client,
			IOptions<DatabaseOptions> dbOptions,
			IOptions<PushNotificationsOptions> pushOptions,
			ObfuscationService obfuscation,
			ILogger<ApnsService> logger,
			IBackgroundTaskQueue taskQueue
		) {
			this.client = client;
			this.dbOptions = dbOptions.Value;
			this.logger = logger;
			this.obfuscation = obfuscation;
			this.taskQueue = taskQueue;
		}
		public void Send(params ApnsNotification[] notifications) {
			taskQueue.QueueBackgroundWorkItem(
				async cancellationToken => {
					var errors = await client.Send(logger, notifications);
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