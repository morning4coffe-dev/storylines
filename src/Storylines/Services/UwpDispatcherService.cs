using Storylines.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Storylines.Services
{
    /// <summary>
    /// UWP / WinUI 2 implementation of <see cref="IDispatcherService"/>. Wraps the application's
    /// main-view <see cref="CoreDispatcher"/> so callers do not need to reference UWP threading
    /// types directly.
    /// </summary>
    internal sealed class UwpDispatcherService : IDispatcherService
    {
        public bool HasThreadAccess => GetDispatcher()?.HasThreadAccess ?? false;

        public Task RunOnUIAsync(Action action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            var dispatcher = GetDispatcher();
            if (dispatcher == null || dispatcher.HasThreadAccess)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object>();
            _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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

            var dispatcher = GetDispatcher();
            if (dispatcher == null || dispatcher.HasThreadAccess)
                return asyncAction();

            var tcs = new TaskCompletionSource<object>();
            _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
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

        private static CoreDispatcher GetDispatcher()
            => CoreApplication.MainView?.CoreWindow?.Dispatcher;
    }
}
