using Storylines.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Storylines.Helpers
{
    /// <summary>
    /// Helpers for safely firing and forgetting <see cref="Task"/>s without losing exceptions.
    /// Replaces ad-hoc <c>_ = SomeAsync()</c> patterns whose exceptions would otherwise be
    /// silently swallowed.
    /// </summary>
    internal static class TaskExtensions
    {
        /// <summary>
        /// Fire-and-forget a task while ensuring exceptions are routed to <paramref name="logger"/>
        /// rather than crashing the app or being lost.
        /// </summary>
        /// <param name="task">The task to observe.</param>
        /// <param name="logger">Logger that receives any exception.</param>
        /// <param name="operation">Short label included in the log entry to aid debugging.</param>
        public static void FireAndForget(this Task task, ILogger logger, string operation)
        {
            if (task is null) return;

            _ = ObserveAsync(task, logger, operation);
        }

        private static async Task ObserveAsync(Task task, ILogger logger, string operation)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is a normal control-flow signal; not an error.
            }
            catch (Exception ex)
            {
                logger?.Warning($"{operation}: {ex.Message}");
            }
        }
    }
}
