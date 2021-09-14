using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Helpers;
using Storylines.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Xaml;

namespace Storylines.Scripts.Functions
{
    class MicrosoftStoreAndAppCenterFunctions
    {
        private static readonly StoreContext storeContext = StoreContext.GetDefault();

        private static DispatcherTimer closeThanksInterval = new DispatcherTimer();

        public static string AppCenterKey { get; } = "";
        public static bool AppCenterEnabled { get; } = false;

        public static async Task CheckForNewUpdateAvailableAsync()
        {
            IReadOnlyList<StorePackageUpdate> updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
            if (updates.Count > 0)
                NotificationManager.DisplayNewUpdateAvailable();
        }
        //https://docs.microsoft.com/en-us/windows/uwp/packaging/self-install-package-updates

        #region Review
        private static DispatcherTimer timer;

        public static void InitializeReview()
        {
            SettingsValues.ReviewPrompt reviewState = (SettingsValues.ReviewPrompt)(Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] ?? 2);
            switch (reviewState)
            {
                 case SettingsValues.ReviewPrompt.NotYet:
                 timer = new DispatcherTimer
                 {
                     Interval = TimeSpan.FromMinutes(35)
                 };
                 timer.Tick += ReviewTimer_Tick;
                 timer.Start();
                 break;
            }
        }

        private static void ReviewTimer_Tick(object sender, object e)
        {
            timer.Stop();

            NotificationManager.DisplayReviewPrompt();
        }

        public static async Task PromptUserToRateApp()
        {
            StoreRateAndReviewResult result = await storeContext.RequestRateAndReviewAppAsync();
            NotificationManager.ClearBadgeNotification();

            switch (result.Status)
            {
                case StoreRateAndReviewStatus.Succeeded:
                    SendAnalyticData_Review("Review status", "Succeeded");

                    AppView.current.reviewRequestInfoBar.IsOpen = false;
                    AppView.current.reviewRequestInfoBar.Visibility = Visibility.Collapsed;
                    NotificationManager.DisplayThankYou();

                    Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] = (int)SettingsValues.ReviewPrompt.SuccessfullyRated;

                    closeThanksInterval.Tick += DispatcherTimer_Tick;
                    closeThanksInterval.Interval = TimeSpan.FromSeconds(8);

                    closeThanksInterval.Start();
                    break;

                case StoreRateAndReviewStatus.CanceledByUser:
                    SendAnalyticData_Review("Review status", "Canceled by user");
                    break;

                case StoreRateAndReviewStatus.NetworkError:
                    SendAnalyticData_Review("Review status", "Network error");
                    break;
            }
        }

        private static void DispatcherTimer_Tick(object sender, object e)
        {
            AppView.current.reviewRequestThankYouInfoBar.IsOpen = false;
            AppView.current.reviewRequestThankYouInfoBar.Visibility = Visibility.Collapsed;
            closeThanksInterval.Stop();
        }
        #endregion

        #region Analytics and Crash
        public static void SendAnalyticData_OnStart()
        {
            Dictionary<string, string> appLaunchSettings = new Dictionary<string, string>()
            {
                { "OS information", $"{SystemInformation.Instance.OperatingSystem} {SystemInformation.Instance.OperatingSystemArchitecture} - {SystemInformation.Instance.DeviceFamily}" },
                { "OS version", $"{SystemInformation.Instance.OperatingSystemVersion.Major}.{SystemInformation.Instance.OperatingSystemVersion.Minor}.{SystemInformation.Instance.OperatingSystemVersion.Build}.{SystemInformation.Instance.OperatingSystemVersion.Revision}" },
                { "App version", SystemInformation.Instance.ApplicationVersion.ToFormattedString() },
                { "First time?", SystemInformation.Instance.IsFirstRun ? SystemInformation.Instance.IsFirstRun.ToString() : SystemInformation.Instance.TotalLaunchCount.ToString() },
                { "Settings - theme", SettingsValues.selectedTheme.ToString() },
                { "Settings - accent", $"{SettingsValues.selectedAccent} ({(SettingsValues.selectedAccent == SettingsValues.SelectedAccent.Custom ? SettingsValues.customAccentColor.ToString() : "")})" },
                { "Settings - autosave", SettingsValues.autosaveEnabled.ToString() },
                { "Settings - exit dialogue enabled", SettingsValues.exitDiagEnabled.ToString() },
                { "Settings - white textbox background", SettingsValues.whiteTextBackground.ToString() },
                { "Settings - new chapter shortcut", SettingsValues.newChapterShortcut.ToString() },
            };

            Analytics.TrackEvent("App launched", appLaunchSettings);
        }

        public static void SendAnalyticData_OnLeave()
        {
            var uptime = SystemInformation.Instance.AppUptime;
            Dictionary<string, string> appLaunchSettings = new Dictionary<string, string>()
            {
                { "Uptime", $"{Math.Round(uptime.TotalHours)}:{Math.Round(uptime.TotalMinutes)}" },
            };

            Analytics.TrackEvent("App leave", appLaunchSettings);
        }

        public static void SendAnalyticData_Review(string name, string text)
        {
            Dictionary<string, string> toSend = new Dictionary<string, string>()
            {
                { name, text },
            };

            Analytics.TrackEvent("Review", toSend);
        }

        public static void SendAnalyticData_FocusMode_Start(bool fullScreen, bool autosave, string measure, string time)
        {
            Dictionary<string, string> toSend = new Dictionary<string, string>()
            {
                { "Entered", "True" },
                { "Fullscreen", fullScreen.ToString() },
                { "Autosave", autosave.ToString() },
                { "Measure", measure },
                { "Time", time },
            };

            Analytics.TrackEvent("Focus Mode", toSend);
        }

        public static void SendAnalyticData_FocusMode_Leave(bool finished)
        {
            Dictionary<string, string> toSend = new Dictionary<string, string>()
            {
                { "Finished?", finished.ToString() },
            };

            Analytics.TrackEvent("Focus Mode", toSend);
        }

        //public static void SendAnalyticData_Feedback(string type, string text, string longText, bool analyticsData)//?
        //{
        //    string id = Guid.NewGuid().ToString();

        //    string toSendString = $"Type: {type}\nShort text: {text}\nLong text: {longText}";
        //    Dictionary<string, string> toSend = new Dictionary<string, string>()
        //    {
        //        { id, toSendString },
        //    };

        //    Analytics.TrackEvent("Feedback dialogue", toSend);
        //}

        public static void SendCrashData_OnUnhandledException(Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Dictionary<string, string> diagnosticInfo = new Dictionary<string, string>()
            {
                { "OS information", $"{SystemInformation.Instance.OperatingSystem} {SystemInformation.Instance.OperatingSystemArchitecture} - {SystemInformation.Instance.DeviceFamily}" },
                { "OS version", $"{SystemInformation.Instance.OperatingSystemVersion.Major}.{SystemInformation.Instance.OperatingSystemVersion.Minor}.{SystemInformation.Instance.OperatingSystemVersion.Build}.{SystemInformation.Instance.OperatingSystemVersion.Revision}" },
                { "Message", e.Message },
                { "Exception", e.Exception?.ToString() },
                { "Culture", SystemInformation.Instance.Culture.EnglishName },
                { "Available memory", SystemInformation.Instance.AvailableMemory.ToString() },
                //{ "First Use Time UTC", SystemInformation.Instance.FirstUseTime.ToUniversalTime().ToString("dd/MM/yyyy HH:mm:ss") },
                { "App version", SystemInformation.Instance.ApplicationVersion.ToFormattedString() },
            };

            ErrorAttachmentLog attachment = ErrorAttachmentLog.AttachmentWithText(
                $"Exception: {e.Exception}, " +
                $"Message: {e.Message}, " +
                $"InnerException: {e.Exception?.InnerException}, " +
                $"InnerExceptionMessage: {e.Exception?.InnerException?.Message}",
                "UnhandledException");

            Analytics.TrackEvent("OnUnhandledException", diagnosticInfo);
            Crashes.TrackError(e.Exception, diagnosticInfo, attachment);
        }
        #endregion
    }
}
