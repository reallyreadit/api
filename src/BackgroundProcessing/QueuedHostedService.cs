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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace api.BackgroundProcessing {
	public class QueuedHostedService : BackgroundService {
		private readonly ILogger<QueuedHostedService> _logger;

		public QueuedHostedService(
			IBackgroundTaskQueue taskQueue,
			ILogger<QueuedHostedService> logger
		) {
			TaskQueue = taskQueue;
			_logger = logger;
		}

		public IBackgroundTaskQueue TaskQueue { get; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
			_logger.LogInformation("Queued Hosted Service is running.");

			await BackgroundProcessing(stoppingToken);
		}

		private async Task BackgroundProcessing(CancellationToken stoppingToken) {
			while (!stoppingToken.IsCancellationRequested) {
				var workItem = await TaskQueue.DequeueAsync(stoppingToken);

				try {
					await workItem(stoppingToken);
				} catch (Exception ex) {
					_logger.LogError(ex, "Error occurred executing {WorkItem}.", nameof(workItem));
				}
			}
		}

		public override async Task StopAsync(CancellationToken stoppingToken) {
			_logger.LogInformation("Queued Hosted Service is stopping.");

			await base.StopAsync(stoppingToken);
		}
	}
}