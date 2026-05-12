using Windows.Services.Store;

namespace Storylines.Helpers;

internal static class MicrosoftStoreFunctions
{
    private static readonly StoreContext _storeContext = StoreContext.GetDefault();
    private static bool _storeContextInitialized;
    private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();
    private static IReadOnlyList<StorePackageUpdate> _availableUpdates = Array.Empty<StorePackageUpdate>();

    private static DispatcherTimer _accumulatorTimer;
    private static bool _hasMandatoryUpdate;
    private static bool _isUpdateInstallInProgress;

    public static async Task CheckForNewUpdateAvailableAsync()
    {
        EnsureStoreContextInitialized();
        IReadOnlyList<StorePackageUpdate> updates = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
        _availableUpdates = updates;
        _hasMandatoryUpdate = false;

        foreach (var update in updates)
        {
            if (update.Mandatory)
            {
                _hasMandatoryUpdate = true;
                break;
            }
        }

        if (updates.Count > 0)
        {
            App.TryGetService<ITelemetryService>()?.TrackStoreUpdateAvailable(updates.Count);
            await RunOnUiThreadAsync(ShowAvailableUpdateState);
        }
        else
        {
            await RunOnUiThreadAsync(NotificationManager.NewUpdateAvailable_Close);
        }
    }

    public static async Task InstallAvailableUpdatesAsync()
    {
        if (_isUpdateInstallInProgress)
            return;

        if (_availableUpdates is null || _availableUpdates.Count < 1)
        {
            await CheckForNewUpdateAvailableAsync();
            if (_availableUpdates is null || _availableUpdates.Count < 1)
                return;
        }

        _isUpdateInstallInProgress = true;

        try
        {
            bool useSilentInstall = _storeContext.CanSilentlyDownloadStorePackageUpdates;
            await RunOnUiThreadAsync(() => ShowInstallingUpdateState(useSilentInstall));

            StorePackageUpdateResult result;
            if (useSilentInstall)
            {
                result = await _storeContext.TrySilentDownloadAndInstallStorePackageUpdatesAsync(_availableUpdates);
            }
            else
            {
                var installOperation = _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(_availableUpdates);
                installOperation.Progress = async (asyncInfo, progress) => await UpdateInstallProgressAsync(progress);
                result = await installOperation.AsTask();
            }

            await RunOnUiThreadAsync(() => HandleInstallResult(result));
        }
        catch (Exception ex)
        {
            _isUpdateInstallInProgress = false;
            App.TryGetService<ILogger>()?.Warning($"Failed to install Store updates: {ex.Message}");

            await RunOnUiThreadAsync(() =>
                ShowAvailableUpdateState(
                    _resources.GetString("storeUpdateFailedTitle"),
                    InfoBarSeverity.Error,
                    _resources.GetString("storeUpdateFailedMessage")));
        }
    }

    public static void InitializeReview()
    {
        var prefs = App.GetService<IPreferencesService>();
        var reviewState = (SettingsValues.ReviewPrompt)prefs.Get(SettingsValueStrings.ReviewPrompt, 2);

        if (reviewState == SettingsValues.ReviewPrompt.SuccessfullyRated ||
            reviewState == SettingsValues.ReviewPrompt.NeverShowAgain)
            return;

        long deferredUntilTicks = prefs.Get<long>(SettingsValueStrings.ReviewDeferredUntil, 0L);
        if (deferredUntilTicks > 0 && DateTime.UtcNow.Ticks < deferredUntilTicks)
            return;

        int sessionCount = prefs.Get<int>(SettingsValueStrings.ReviewSessionCount, 0) + 1;
        prefs.Set(SettingsValueStrings.ReviewSessionCount, sessionCount);

        _accumulatorTimer ??= new DispatcherTimer();
        _accumulatorTimer.Stop();
        _accumulatorTimer.Interval = TimeSpan.FromMinutes(1);
        _accumulatorTimer.Tick -= OnAccumulatorTimerTick;
        _accumulatorTimer.Tick += OnAccumulatorTimerTick;
        _accumulatorTimer.Start();
    }

    private static void OnAccumulatorTimerTick(object sender, object e)
    {
        var prefs = App.GetService<IPreferencesService>();

        int accumulated = prefs.Get<int>(SettingsValueStrings.ReviewAccumulatedMinutes, 0) + 1;
        prefs.Set(SettingsValueStrings.ReviewAccumulatedMinutes, accumulated);

        int sessionCount = prefs.Get<int>(SettingsValueStrings.ReviewSessionCount, 0);
        if (accumulated >= 60 && sessionCount >= 3)
            TryShowReviewPrompt("cumulative_time");
    }

    public static void OnExportCompleted()
    {
        TryShowReviewPrompt("export_milestone");
    }

    public static void StopReviewTimer()
    {
        _accumulatorTimer?.Stop();
    }

    private static void TryShowReviewPrompt(string source)
    {
        var prefs = App.GetService<IPreferencesService>();
        var reviewState = (SettingsValues.ReviewPrompt)prefs.Get(SettingsValueStrings.ReviewPrompt, 2);

        if (reviewState == SettingsValues.ReviewPrompt.SuccessfullyRated ||
            reviewState == SettingsValues.ReviewPrompt.NeverShowAgain)
            return;

        long deferredUntilTicks = prefs.Get<long>(SettingsValueStrings.ReviewDeferredUntil, 0L);
        if (deferredUntilTicks > 0 && DateTime.UtcNow.Ticks < deferredUntilTicks)
            return;

        _accumulatorTimer?.Stop();

        DisplayReviewPrompt(source);
    }

    private static void DisplayReviewPrompt(string source)
    {
        App.TryGetService<ITelemetryService>()?.TrackReviewPromptDisplayed(source);

        var notifications = App.GetService<INotificationService>();
        notifications.ShowPersistentNotification(new PersistentNotificationRequest
        {
            Severity = InfoBarSeverity.Informational,
            Title = _resources.GetString("experience"),
            Message = _resources.GetString("reviewDialog"),
            IconSource = new FontIconSource { Glyph = "\uE734" },
            IsClosable = true,
            OnClosed = OnReviewDismissed,
            Width = 420,
            Buttons = new[]
            {
                new NotificationButton
                {
                    Label = _resources.GetString("reviewDialogOption1"),
                    OnClick = () => _ = PromptUserToRateAppAsync("review_infobar"),
                },
                new NotificationButton
                {
                    Label = _resources.GetString("reviewDialogOption2"),
                    OnClick = OnReviewNotNow,
                },
                new NotificationButton
                {
                    Label = _resources.GetString("reviewDialogOption3"),
                    OnClick = OnReviewNeverShowAgain,
                },
            },
        });
    }

    internal static void ShowReviewPromptPreview()
    {
        DisplayReviewPrompt("developer_tools");
    }

    private static void OnReviewDismissed()
    {
        App.TryGetService<ITelemetryService>()?.TrackReviewInteraction("review_infobar", "dismissed");
        App.GetService<IPreferencesService>().Set(SettingsValueStrings.ReviewDeferredUntil, DateTime.UtcNow.AddDays(14).Ticks);
        StopReviewTimer();
    }

    private static void OnReviewNotNow()
    {
        App.TryGetService<ITelemetryService>()?.TrackReviewInteraction("review_infobar", "not_now");
        App.GetService<IPreferencesService>().Set(SettingsValueStrings.ReviewDeferredUntil, DateTime.UtcNow.AddDays(14).Ticks);
        App.GetService<INotificationService>().DismissPersistentNotification();
        StopReviewTimer();
    }

    private static void OnReviewNeverShowAgain()
    {
        App.TryGetService<ITelemetryService>()?.TrackReviewInteraction("review_infobar", "never_show_again");
        App.GetService<IPreferencesService>().Set(SettingsValueStrings.ReviewPrompt, (int)SettingsValues.ReviewPrompt.NeverShowAgain);
        App.GetService<INotificationService>().DismissPersistentNotification();
    }

    public static async Task PromptUserToRateAppAsync(string source = "unknown")
    {
        var telemetry = App.TryGetService<ITelemetryService>();
        StoreRateAndReviewResult result = await _storeContext.RequestRateAndReviewAppAsync();

        switch (result.Status)
        {
            case StoreRateAndReviewStatus.Succeeded:
                telemetry?.TrackReviewInteraction(source, "completed", "succeeded");

                App.GetService<INotificationService>().DismissPersistentNotification();
                App.GetService<INotificationService>().ShowNotification(new NotificationRequest
                {
                    Severity = InfoBarSeverity.Success,
                    Title = _resources.GetString("reviewRequestThankYou.Title"),
                    Duration = TimeSpan.FromSeconds(8),
                });

                App.GetService<IPreferencesService>().Set(SettingsValueStrings.ReviewPrompt, (int)SettingsValues.ReviewPrompt.SuccessfullyRated);
                break;

            case StoreRateAndReviewStatus.CanceledByUser:
                telemetry?.TrackReviewInteraction(source, "dismissed", "canceled_by_user");
                break;

            case StoreRateAndReviewStatus.NetworkError:
                telemetry?.TrackReviewInteraction(source, "failed", "network_error");
                break;

            default:
                telemetry?.TrackReviewInteraction(source, "completed", result.Status.ToString());
                break;
        }
    }

    private static async Task UpdateInstallProgressAsync(StorePackageUpdateStatus progress)
    {
        double progressValue = Math.Max(0, Math.Min(100, progress.PackageDownloadProgress * 100));

        await RunOnUiThreadAsync(() =>
            App.GetService<INotificationService>().UpdatePersistentNotificationProgress(progressValue));
    }

    private static void HandleInstallResult(StorePackageUpdateResult result)
    {
        _isUpdateInstallInProgress = false;

        switch (result.OverallState)
        {
            case StorePackageUpdateState.Completed:
                _availableUpdates = Array.Empty<StorePackageUpdate>();
                _hasMandatoryUpdate = false;
                ShowUpdateNotification(
                    _resources.GetString("storeUpdateInstalledTitle"),
                    InfoBarSeverity.Success,
                    _resources.GetString("storeUpdateInstalledMessage"),
                    detail: string.Empty,
                    showActions: false,
                    showProgressBar: false,
                    isProgressIndeterminate: false);
                NotificationManager.ClearBadgeNotification();
                break;
            case StorePackageUpdateState.Canceled:
                ShowAvailableUpdateState(
                    _resources.GetString("storeUpdateCancelledTitle"),
                    InfoBarSeverity.Warning,
                    _resources.GetString("storeUpdateCancelledMessage"));
                break;
            default:
                ShowAvailableUpdateState(
                    _resources.GetString("storeUpdateFailedTitle"),
                    InfoBarSeverity.Error,
                    _resources.GetString("storeUpdateFailedMessage"));
                break;
        }
    }

    private static void ShowAvailableUpdateState()
    {
        ShowUpdateNotification(
            _resources.GetString("updateAvailableTitle.Title"),
            _hasMandatoryUpdate ? InfoBarSeverity.Warning : InfoBarSeverity.Informational,
            _resources.GetString("storeUpdateAvailableMessage"),
            _hasMandatoryUpdate ? _resources.GetString("storeUpdateMandatoryMessage") : string.Empty,
            showActions: true,
            showProgressBar: false,
            isProgressIndeterminate: false);
    }

    private static void ShowAvailableUpdateState(string title, InfoBarSeverity severity, string message)
    {
        ShowUpdateNotification(
            title,
            severity,
            message,
            _hasMandatoryUpdate ? _resources.GetString("storeUpdateMandatoryMessage") : string.Empty,
            showActions: true,
            showProgressBar: false,
            isProgressIndeterminate: false);
    }

    private static void ShowInstallingUpdateState(bool useSilentInstall)
    {
        ShowUpdateNotification(
            _resources.GetString("updateAvailableTitle.Title"),
            InfoBarSeverity.Informational,
            _resources.GetString("storeUpdateInstallingMessage"),
            detail: string.Empty,
            showActions: false,
            showProgressBar: true,
            isProgressIndeterminate: useSilentInstall);
    }

    private static void ShowUpdateNotification(
        string title,
        InfoBarSeverity severity,
        string message,
        string detail,
        bool showActions,
        bool showProgressBar,
        bool isProgressIndeterminate)
    {
        IReadOnlyList<NotificationButton> buttons = showActions
            ? new[]
              {
                  new NotificationButton
                  {
                      Label = _resources.GetString("storeUpdateInstallNow"),
                      OnClick = () => _ = InstallAvailableUpdatesAsync(),
                  },
                  new NotificationButton
                  {
                      Label = _resources.GetString("storeUpdateLater"),
                      OnClick = NotificationManager.NewUpdateAvailable_Close,
                  },
              }
            : null;

        App.GetService<INotificationService>().ShowPersistentNotification(new PersistentNotificationRequest
        {
            Title = title,
            Severity = severity,
            Message = message,
            Detail = detail,
            IconSource = new FontIconSource { Glyph = "\uE896" },
            IsClosable = !_isUpdateInstallInProgress,
            OnClosed = NotificationManager.NewUpdateAvailable_Close,
            Buttons = buttons,
            HasProgressBar = showProgressBar,
            IsProgressIndeterminate = isProgressIndeterminate,
            Width = 440,
        });

        NotificationManager.DisplayBadgeNotification("attention");
    }

    private static void EnsureStoreContextInitialized()
    {
        var windowCtx = App.TryGetService<IWindowManager>()?.PrimaryWindow;
        if (!_storeContextInitialized && windowCtx?.Window is not null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(windowCtx.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(_storeContext, hwnd);
            _storeContextInitialized = true;
        }
    }

    private static Task RunOnUiThreadAsync(Action action)
    {
        var appView = App.GetService<WindowContext>()?.AppView;
        if (appView?.DispatcherQueue is null)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<object>();
        appView.DispatcherQueue.TryEnqueue(() =>
        {
            action();
            tcs.SetResult(null);
        });
        return tcs.Task;
    }
}
