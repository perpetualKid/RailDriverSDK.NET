using System.Threading;
using System.Threading.Tasks;

namespace RailDriver
{
    internal sealed class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> taskCompletionSource = CreateTaskCompletionSource();

        public Task WaitAsync(CancellationToken token)
        {
            Task waitTask = taskCompletionSource.Task;
            if (!token.CanBeCanceled || waitTask.IsCompleted)
                return waitTask;

            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);

            return WaitAsync(waitTask, token);
        }

        public void Set()
        {
            taskCompletionSource.TrySetResult(true);
        }

        public void Reset()
        {
            while (true)
            {
                TaskCompletionSource<bool> tcs = taskCompletionSource;
                if (!tcs.Task.IsCompleted ||
                    Interlocked.CompareExchange(ref taskCompletionSource, CreateTaskCompletionSource(), tcs) == tcs)
                    return;
            }
        }

        private static TaskCompletionSource<bool> CreateTaskCompletionSource()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private static async Task WaitAsync(Task waitTask, CancellationToken token)
        {
            TaskCompletionSource<bool> cancellationTaskSource = CreateTaskCompletionSource();
            using (token.Register(state => ((TaskCompletionSource<bool>)state).TrySetCanceled(), cancellationTaskSource))
            {
                Task completedTask = await Task.WhenAny(waitTask, cancellationTaskSource.Task).ConfigureAwait(false);
                await completedTask.ConfigureAwait(false);
            }
        }
    }
}
