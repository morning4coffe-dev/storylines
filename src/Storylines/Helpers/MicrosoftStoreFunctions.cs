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

        private static DispatcherTimer _reviewTimer;
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

            if (_availableUpdates == null || _availableUpdates.Count < 1)
            {
                await CheckForNewUpdateAvailableAsync();
                if (_availableUpdates == null || _availableUpdates.Count < 1)
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
            SettingsValues.ReviewPrompt reviewState = (SettingsValues.ReviewPrompt)(Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] ?? 2);
            if (reviewState != SettingsValues.ReviewPrompt.NotYet)
                return;

            _reviewTimer ??= new DispatcherTimer();
            _reviewTimer.Stop();
            _reviewTimer.Interval = TimeSpan.FromMinutes(35);
            _reviewTimer.Tick -= ReviewTimer_Tick;
            _reviewTimer.Tick += ReviewTimer_Tick;
            _reviewTimer.Start();
        }

        private static void ReviewTimer_Tick(object sender, object e)
        {
            _reviewTimer?.Stop();
            NotificationManager.DisplayReviewPrompt();
        }

        public static async Task PromptUserToRateAppAsync(string source = "unknown")
        {
            var telemetry = App.TryGetService<ITelemetryService>();
            StoreRateAndReviewResult result = await _storeContext.RequestRateAndReviewAppAsync();
            NotificationManager.ClearBadgeNotification();

            switch (result.Status)
            {
                case StoreRateAndReviewStatus.Succeeded:
                    telemetry?.TrackReviewInteraction(source, "completed", "succeeded");

                    AppView.current.reviewRequestInfoBar.IsOpen = false;
                    AppView.current.reviewRequestInfoBar.Visibility = Visibility.Collapsed;
                    NotificationManager.DisplayThankYou();

                    Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] = (int)SettingsValues.ReviewPrompt.SuccessfullyRated;

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
            AppView.current.reviewRequestThankYouInfoBar.IsOpen = false;
            AppView.current.reviewRequestThankYouInfoBar.Visibility = Visibility.Collapsed;

            _closeThanksInterval.Stop();
            _closeThanksInterval.Tick -= CloseThanksInterval_Tick;
        }

        private static async Task UpdateInstallProgressAsync(StorePackageUpdateStatus progress)
        {
            double progressValue = Math.Max(0, Math.Min(100, progress.PackageDownloadProgress * 100));

            await RunOnUiThreadAsync(() =>
            {
                if (AppView.current?.updateAvailableProgressBar == null)
                    return;

                AppView.current.updateAvailableProgressBar.IsIndeterminate = false;
                AppView.current.updateAvailableProgressBar.Value = progressValue;
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
            if (AppView.current == null)
                return;

            AppView.current.updateAvailableInfoBar.Title = title;
            AppView.current.updateAvailableInfoBar.Severity = severity;
            AppView.current.updateAvailableInfoBar.RequestedTheme = AppView.current.ActualTheme;
            AppView.current.updateAvailableInfoBar.IsClosable = !_isUpdateInstallInProgress;

            AppView.current.updateAvailableInfoBarText.Text = message;
            AppView.current.updateAvailableInfoBarDetailText.Text = detail;
            AppView.current.updateAvailableInfoBarDetailText.Visibility =
                string.IsNullOrWhiteSpace(detail) ? Visibility.Collapsed : Visibility.Visible;

            AppView.current.updateAvailableProgressBar.Value = 0;
            AppView.current.updateAvailableProgressBar.IsIndeterminate = isProgressIndeterminate;
            AppView.current.updateAvailableProgressBar.Visibility =
                showProgressBar ? Visibility.Visible : Visibility.Collapsed;

            AppView.current.updateAvailableActionsPanel.Visibility =
                showActions ? Visibility.Visible : Visibility.Collapsed;
            AppView.current.updateAvailablePrimaryButton.IsEnabled = !_isUpdateInstallInProgress;
            AppView.current.updateAvailableSecondaryButton.IsEnabled = !_isUpdateInstallInProgress;

            NotificationManager.DisplayNewUpdateAvailable();
        }

        private static void EnsureStoreContextInitialized()
        {
            var windowCtx = App.TryGetService<IWindowManager>()?.PrimaryWindow;
            if (!_storeContextInitialized && windowCtx?.Window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(windowCtx.Window);
                WinRT.Interop.InitializeWithWindow.Initialize(_storeContext, hwnd);
                _storeContextInitialized = true;
            }
        }

        private static Task RunOnUiThreadAsync(Action action)
        {
            if (AppView.current?.DispatcherQueue == null)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<object>();
            AppView.current.DispatcherQueue.TryEnqueue(() =>
            {
                action();
                tcs.SetResult(null);
            });
            return tcs.Task;
        }
    }
}