using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.UI.Xaml;

namespace Storylines.Helpers
{
    class NotificationManager
    {
        public enum InAppNotificationType { None, NewUpdate, Review, ThankYou };
        private static InAppNotificationType currentInAppNotification = InAppNotificationType.None;

        private static WindowContext WindowContext => App.GetService<WindowContext>();
        private static AppView Shell => WindowContext.AppView;

        public static void DisplayBadgeNotification(string badgeGlyphValue)
        {
            XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(short.TryParse(badgeGlyphValue, out _) ? BadgeTemplateType.BadgeNumber : BadgeTemplateType.BadgeGlyph);

            XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
            badgeElement.SetAttribute("value", badgeGlyphValue);

            BadgeNotification badge = new BadgeNotification(badgeXml);
            BadgeUpdater badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();

            badgeUpdater.Update(badge);
        }

        public static void ClearBadgeNotification()
        {
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
        }

        public static void DisplayNewUpdateAvailable()
        {
            currentInAppNotification = InAppNotificationType.NewUpdate;
            Shell.updateAvailableInfoBar.RequestedTheme = Shell.ActualTheme;
            Shell.updateAvailableInfoBar.IsOpen = true;
            Shell.updateAvailableInfoBar.Visibility = Visibility.Visible;

            DisplayBadgeNotification("attention");
        }

        public static void NewUpdateAvailable_Close()
        {
            currentInAppNotification = InAppNotificationType.None;
            Shell.updateAvailableInfoBar.IsOpen = false;
            Shell.updateAvailableInfoBar.Visibility = Visibility.Collapsed;
            ClearBadgeNotification();
        }

        public static void DisplayReviewPrompt(string source = "unknown")
        {
            App.TryGetService<ITelemetryService>()?.TrackReviewPromptDisplayed(source);
            Shell.reviewRequestInfoBar.IsOpen = true;
            Shell.reviewRequestInfoBar.Visibility = Visibility.Visible;
            Shell.reviewRequestInfoBar.RequestedTheme = Shell.ActualTheme;
        }

        public static void DisplayThankYou()
        {
            Shell.reviewRequestThankYouInfoBar.IsOpen = true;
            Shell.reviewRequestThankYouInfoBar.Visibility = Visibility.Visible;
            Shell.reviewRequestThankYouInfoBar.RequestedTheme = Shell.ActualTheme;
        }
    }
}
