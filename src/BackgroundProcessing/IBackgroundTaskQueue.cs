using System;
using System.Threading;
using System.Threading.Tasks;

namespace api.BackgroundProcessing {
	public interface IBackgroundTaskQueue {
		void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

		Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
	}
}