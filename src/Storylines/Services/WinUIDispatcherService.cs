using Microsoft.UI.Dispatching;
using Storylines.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Storylines.Services
{
    /// <summary>
    /// WinUI 3 implementation of <see cref="IDispatcherService"/>. Wraps the application's
    /// <see cref="DispatcherQueue"/> so callers do not need to reference WinUI threading
    /// types directly.
    /// </summary>
    internal sealed class WinUIDispatcherService : IDispatcherService
    {
        private readonly DispatcherQueue _dispatcherQueue;

        public WinUIDispatcherService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public bool HasThreadAccess => _dispatcherQueue?.HasThreadAccess ?? false;

        public Task RunOnUIAsync(Action action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object>();
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        public Task RunOnUIAsync(Func<Task> asyncAction)
        {
            if (asyncAction is null) throw new ArgumentNullException(nameof(asyncAction));

            if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess)
                return asyncAction();

            var tcs = new TaskCompletionSource<object>();
            _dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await asyncAction();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
