#if DEBUG
#nullable enable
using System;
using System.Threading.Tasks;
using Storylines.Services;

namespace Storylines.Testing;

internal static partial class ApplicationE2EHook
{
    private static readonly Func<bool>? HasPendingRequestCallback;
    private static readonly Func<WindowContext, Task>? RunAsyncCallback;

    static ApplicationE2EHook()
    {
        Func<bool>? hasPendingRequest = null;
        Func<WindowContext, Task>? runAsync = null;
        Configure(ref hasPendingRequest, ref runAsync);
        HasPendingRequestCallback = hasPendingRequest;
        RunAsyncCallback = runAsync;
    }

    public static bool HasPendingRequest => HasPendingRequestCallback?.Invoke() == true;

    public static Task RunAsync(WindowContext context) =>
        RunAsyncCallback?.Invoke(context) ?? Task.CompletedTask;

    static partial void Configure(
        ref Func<bool>? hasPendingRequest,
        ref Func<WindowContext, Task>? runAsync);
}
#endif
