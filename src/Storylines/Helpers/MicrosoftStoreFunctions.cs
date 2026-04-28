using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Xaml;

namespace Storylines.Helpers
{
    internal static class MicrosoftStoreFunctions
    {
        private static readonly StoreContext _storeContext = StoreContext.GetDefault();
        private static readonly DispatcherTimer _closeThanksInterval = new DispatcherTimer();

        private static DispatcherTimer _reviewTimer;

        public static async Task CheckForNewUpdateAvailableAsync()
        {
            IReadOnlyList<StorePackageUpdate> updates = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
            if (updates.Count > 0)
            {
                App.TryGetService<ITelemetryService>()?.TrackStoreUpdateAvailable(updates.Count);
                NotificationManager.DisplayNewUpdateAvailable();
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
    }
}