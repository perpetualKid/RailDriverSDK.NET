using System.Threading;
using System.Threading.Tasks;

namespace RailDriver
{
    internal sealed class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        public Task WaitAsync(CancellationToken token)
        {
            token.Register(() =>
            {
                TaskCompletionSource<bool> tcs = taskCompletionSource;
                Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s).TrySetResult(false),
                    tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
            });
            return taskCompletionSource.Task;
        }

        public void Set()
        {
            TaskCompletionSource<bool> tcs = taskCompletionSource;
            Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s).TrySetResult(true),
                tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
            tcs.Task.Wait();
        }

        public void Reset()
        {
            while (true)
            {
                TaskCompletionSource<bool> tcs = taskCompletionSource;
                if (!tcs.Task.IsCompleted ||
                    Interlocked.CompareExchange(ref taskCompletionSource, new TaskCompletionSource<bool>(), tcs) == tcs)
                    return;
            }
        }
    }
}
