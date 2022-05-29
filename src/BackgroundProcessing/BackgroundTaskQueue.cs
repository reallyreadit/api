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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace api.BackgroundProcessing {
	public class BackgroundTaskQueue : IBackgroundTaskQueue {
		private ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
		private SemaphoreSlim _signal = new SemaphoreSlim(0);

		public void QueueBackgroundWorkItem(
			Func<CancellationToken, Task> workItem
		) {
			if (workItem == null) {
				throw new ArgumentNullException(nameof(workItem));
			}

			_workItems.Enqueue(workItem);
			_signal.Release();
		}

		public async Task<Func<CancellationToken, Task>> DequeueAsync(
			CancellationToken cancellationToken
		) {
			await _signal.WaitAsync(cancellationToken);
			_workItems.TryDequeue(out var workItem);

			return workItem;
		}
	}
}