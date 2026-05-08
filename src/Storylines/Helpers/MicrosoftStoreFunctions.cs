using Microsoft.UI.Xaml.Controls;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Services.Store;
using Microsoft.UI.Xaml;

namespace Storylines.Helpers
{
    internal static class MicrosoftStoreFunctions
    {
        private static readonly StoreContext _storeContext = StoreContext.GetDefault();
        private static bool _storeContextInitialized;
        private static readonly DispatcherTimer _closeThanksInterval = new DispatcherTimer();
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

            NotificationManager.DisplayReviewPrompt(source);
        }

        public static async Task PromptUserToRateAppAsync(string source = "unknown")
        {
            var telemetry = App.TryGetService<ITelemetryService>();
            StoreRateAndReviewResult result = await _storeContext.RequestRateAndReviewAppAsync();

            switch (result.Status)
            {
                case StoreRateAndReviewStatus.Succeeded:
                    telemetry?.TrackReviewInteraction(source, "completed", "succeeded");

                    var appView = App.GetService<WindowContext>().AppView;
                    appView.reviewRequestInfoBar.IsOpen = false;
                    appView.reviewRequestInfoBar.Visibility = Visibility.Collapsed;
                    NotificationManager.DisplayThankYou();

                    App.GetService<Storylines.Services.Interfaces.IPreferencesService>().Set(SettingsValueStrings.ReviewPrompt, (int)SettingsValues.ReviewPrompt.SuccessfullyRated);

                    _closeThanksInterval.Tick -= CloseThanksInterval_Tick;
                    _closeThanksInterval.Tick += CloseThanksInterval_Tick;
                    _closeThanksInterval.Interval = TimeSpan.FromSeconds(8);
                    _closeThanksInterval.Start();
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

        private static void CloseThanksInterval_Tick(object sender, object e)
        {
            var appView = App.GetService<WindowContext>().AppView;
            appView.reviewRequestThankYouInfoBar.IsOpen = false;
            appView.reviewRequestThankYouInfoBar.Visibility = Visibility.Collapsed;

            _closeThanksInterval.Stop();
            _closeThanksInterval.Tick -= CloseThanksInterval_Tick;
        }

        private static async Task UpdateInstallProgressAsync(StorePackageUpdateStatus progress)
        {
            double progressValue = Math.Max(0, Math.Min(100, progress.PackageDownloadProgress * 100));

            await RunOnUiThreadAsync(() =>
            {
                var appView = App.GetService<WindowContext>()?.AppView;
                if (appView?.updateAvailableProgressBar is null)
                    return;

                appView.updateAvailableProgressBar.IsIndeterminate = false;
                appView.updateAvailableProgressBar.Value = progressValue;
            });
        }

        private static void HandleInstallResult(StorePackageUpdateResult result)
        {
            _isUpdateInstallInProgress = false;

            switch (result.OverallState)
            {
                case StorePackageUpdateState.Completed:
                    _availableUpdates = Array.Empty<StorePackageUpdate>();
                    _hasMandatoryUpdate = false;
                    ShowUpdateInfoBar(
                        _resources.GetString("storeUpdateInstalledTitle"),
                        InfoBarSeverity.Success,
                        _resources.GetString("storeUpdateInstalledMessage"),
                        string.Empty,
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
            ShowUpdateInfoBar(
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
            ShowUpdateInfoBar(
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
            ShowUpdateInfoBar(
                _resources.GetString("updateAvailableTitle.Title"),
                InfoBarSeverity.Informational,
                _resources.GetString("storeUpdateInstallingMessage"),
                string.Empty,
                showActions: false,
                showProgressBar: true,
                isProgressIndeterminate: useSilentInstall);
        }

        private static void ShowUpdateInfoBar(
            string title,
            InfoBarSeverity severity,
            string message,
            string detail,
            bool showActions,
            bool showProgressBar,
            bool isProgressIndeterminate)
        {
            var appView = App.GetService<WindowContext>()?.AppView;
            if (appView is null)
                return;

            appView.updateAvailableInfoBar.Title = title;
            appView.updateAvailableInfoBar.Severity = severity;
            appView.updateAvailableInfoBar.RequestedTheme = appView.ActualTheme;
            appView.updateAvailableInfoBar.IsClosable = !_isUpdateInstallInProgress;

            appView.updateAvailableInfoBarText.Text = message;
            appView.updateAvailableInfoBarDetailText.Text = detail;
            appView.updateAvailableInfoBarDetailText.Visibility =
                string.IsNullOrWhiteSpace(detail) ? Visibility.Collapsed : Visibility.Visible;

            appView.updateAvailableProgressBar.Value = 0;
            appView.updateAvailableProgressBar.IsIndeterminate = isProgressIndeterminate;
            appView.updateAvailableProgressBar.Visibility =
                showProgressBar ? Visibility.Visible : Visibility.Collapsed;

            appView.updateAvailableActionsPanel.Visibility =
                showActions ? Visibility.Visible : Visibility.Collapsed;
            appView.updateAvailablePrimaryButton.IsEnabled = !_isUpdateInstallInProgress;
            appView.updateAvailableSecondaryButton.IsEnabled = !_isUpdateInstallInProgress;

            NotificationManager.DisplayNewUpdateAvailable();
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
}