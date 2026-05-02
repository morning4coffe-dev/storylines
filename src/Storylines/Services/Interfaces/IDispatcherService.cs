using System;
using System.Threading.Tasks;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Abstraction over the UI dispatcher that decouples view-models and services from
    /// platform-specific UI threading types (e.g. <c>CoreDispatcher</c> on UWP,
    /// <c>DispatcherQueue</c> on WinUI 3). All UI-thread marshalling should flow through this
    /// interface so consumers remain platform-portable for the planned WinUI 3 / multi-platform
    /// migration.
    /// </summary>
    public interface IDispatcherService
    {
        /// <summary>
        /// Gets a value indicating whether the calling thread already has access to the UI thread.
        /// </summary>
        bool HasThreadAccess { get; }

        /// <summary>
        /// Schedules <paramref name="action"/> to run on the UI thread and returns a task that
        /// completes when the action has finished executing.
        /// </summary>
        Task RunOnUIAsync(Action action);

        /// <summary>
        /// Schedules <paramref name="asyncAction"/> on the UI thread and awaits its completion.
        /// </summary>
        Task RunOnUIAsync(Func<Task> asyncAction);
    }
}
