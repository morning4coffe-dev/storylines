using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Scripts.Functions
{
    class NotificationManager
    {
        public enum InAppNotificationType { None, NewUpdate, Review, ThankYou };
        public static InAppNotificationType currentInAppNotification = InAppNotificationType.None;

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

        public static void DisplayMainProgressBar(bool isIndeterminate)
        { 
            MainPage.Current.mainProgressBar.Visibility = Visibility.Visible;
            MainPage.Current.mainProgressBar.IsIndeterminate = isIndeterminate;

            UpdateMainProgressBar(0, ProgressState.Normal);
        }

        public enum ProgressState { Normal, Paused, Error };
        public static void UpdateMainProgressBar(int value, ProgressState state)
        {
            MainPage.Current.mainProgressBar.Value = value;

            switch (state)
            {
                case ProgressState.Paused:
                    MainPage.Current.mainProgressBar.ShowPaused = true;
                    MainPage.Current.mainProgressBar.ShowError = false;
                    break;
                case ProgressState.Error:
                    MainPage.Current.mainProgressBar.ShowPaused = false;
                    MainPage.Current.mainProgressBar.ShowError = true;
                    break;
                default:
                    MainPage.Current.mainProgressBar.ShowPaused = false;
                    MainPage.Current.mainProgressBar.ShowError = false;
                    break;
            }
        }

        public static void HideMainProgressBar()
        {
            MainPage.Current.mainProgressBar.Visibility = Visibility.Collapsed;
        }

        public static void DisplayNewUpdateAvailable()
        {
            AppView.current.updateAvailableInfoBar.IsOpen = true;
            AppView.current.updateAvailableInfoBar.Visibility = Visibility.Visible;

            DisplayBadgeNotification("attention");
        }

        public static void NewUpdateAvailable_Close()
        {
            AppView.current.updateAvailableInfoBar.IsOpen = false;
            AppView.current.updateAvailableInfoBar.Visibility = Visibility.Collapsed;
            ClearBadgeNotification();
        }

        public static void DisplayReviewPrompt()
        {
            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_Review("Review status", "Shown");
            AppView.current.reviewRequestInfoBar.IsOpen = true;
            AppView.current.reviewRequestInfoBar.Visibility = Visibility.Visible;
            AppView.current.reviewRequestInfoBar.RequestedTheme = AppView.current.RequestedTheme;

            DisplayBadgeNotification("attention");
        }

        public static void DisplayThankYou()
        {
            AppView.current.reviewRequestThankYouInfoBar.IsOpen = true;
            AppView.current.reviewRequestThankYouInfoBar.Visibility = Visibility.Visible;
            AppView.current.reviewRequestThankYouInfoBar.RequestedTheme = AppView.current.RequestedTheme;
        }

        private static DispatcherTimer InAppNotificationTimer;

        public static void DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, string text, string longText)
        {
            AppView.current.alertNotificationInfoBar.IsOpen = true;
            AppView.current.alertNotificationInfoBar.Visibility = Visibility.Visible;

            AppView.current.alertNotificationInfoBar.Severity = severity;
            AppView.current.alertNotificationInfoBar.Title = text;

            AppView.current.alertNotificationInfoBar.RequestedTheme = AppView.current.RequestedTheme;

            if (longText.Length < 1)
                AppView.current.alertNotificationInfoBarTextStack.Visibility = Visibility.Collapsed;
            else
            {
                AppView.current.alertNotificationInfoBarTextStack.Visibility = Visibility.Visible;
                AppView.current.alertNotificationInfoBarText.Text = longText;
            }

            if (InAppNotificationTimer != null)
            {
                InAppNotificationTimer = null;
            }

            InAppNotificationTimer = new DispatcherTimer();
            InAppNotificationTimer.Tick += InAppNotificationTimer_Tick;
            InAppNotificationTimer.Interval = TimeSpan.FromSeconds(12);
            InAppNotificationTimer.Start();

            DisplayBadgeNotification("attention");
        }

        private static void InAppNotificationTimer_Tick(object sender, object e)
        {
            (sender as DispatcherTimer).Stop();

            if (InAppNotificationTimer != null)
            {
                AppView.current.alertNotificationInfoBar.Visibility = Visibility.Collapsed;
                AppView.current.alertNotificationInfoBar.IsOpen = false;

                InAppNotification_Close();
            }
        }

        public static void InAppNotification_Close()
        {
            InAppNotificationTimer.Tick -= InAppNotificationTimer_Tick;
            InAppNotificationTimer.Stop();
            InAppNotificationTimer = null;
            ClearBadgeNotification();
        }

        private static void CheckForOpenDialogueAndClose()
        {
            if (AppView.currentlyOpenedDialogue != null)
            {
                if (AppView.currentlyOpenedDialogue == LoadProjectDialogue.loadFile)
                    LoadProjectDialogue.loadFile.isEscape = false;

                AppView.currentlyOpenedDialogue.Hide();
            }
        }

        public static async Task DisplayUnsavedProgressDialogue(bool appClosing)
        {
            CheckForOpenDialogueAndClose();

            ContentDialog exitDialog = new ContentDialog
            {
                Title = ResourceLoader.GetForCurrentView().GetString("exitWithoutSaveDialogTitle"),
                Content = ResourceLoader.GetForCurrentView().GetString("exitWithoutSaveDialogDescription"),
                PrimaryButtonText = ResourceLoader.GetForCurrentView().GetString("exitWithoutSaveDialogSave"),
                SecondaryButtonText = ResourceLoader.GetForCurrentView().GetString("exitWithoutSaveDialogDontSave"),
                CloseButtonText = ResourceLoader.GetForCurrentView().GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = MainPage.Current.RequestedTheme,
                //PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"]
            };
            AppView.currentlyOpenedDialogue = exitDialog;
            exitDialog.RequestedTheme = AppView.current.ActualTheme;

            ContentDialogResult result = await exitDialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    exitDialog.Hide();
                    SaveSystem.SaveAndExitOrClearAll(appClosing);
                    break;
                case ContentDialogResult.Secondary:
                    if (appClosing)
                        App.Current.Exit();
                    else
                    {
                        SaveSystem.currentProject.file = null;
                        AppView.current.ClearEverything();
                        TimeTravelSystem.unSavedProgress = false;

                        LoadProjectDialogue.Open();
                        exitDialog.Hide();
                    }
                    break;
            }
            AppView.currentlyOpenedDialogue = null;
        }

        public static async Task DisplayNotFinishedInFocusModeDialogue()
        {
            CheckForOpenDialogueAndClose();

            ContentDialog leaveDialog = new ContentDialog
            {
                Title = ResourceLoader.GetForCurrentView().GetString("FocusModeLeaveDialogueTitle"),
                Content = ResourceLoader.GetForCurrentView().GetString("FocusModeLeaveDialogueDescription"),
                PrimaryButtonText = ResourceLoader.GetForCurrentView().GetString("FocusModeLeaveDialogueStay"),
                SecondaryButtonText = ResourceLoader.GetForCurrentView().GetString("FocusModeLeaveDialogueLeave"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = MainPage.Current.RequestedTheme,
            };
            AppView.currentlyOpenedDialogue = leaveDialog;
            leaveDialog.RequestedTheme = AppView.current.ActualTheme;

            ContentDialogResult result = await leaveDialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    leaveDialog.Hide();
                    break;
                case ContentDialogResult.Secondary:
                    MainPage.FocusMode.Leave();
                    break;
            }
            AppView.currentlyOpenedDialogue = null;
        }

        public static async Task DisplayNotAppliedChangesCharactersPageDialogue(bool appClosing)
        {
            CheckForOpenDialogueAndClose();

            ContentDialog leaveDialog = new ContentDialog
            {
                Title = ResourceLoader.GetForCurrentView().GetString("changesCharactersPageDialogueTitle"),
                Content = ResourceLoader.GetForCurrentView().GetString("changesCharactersPageDialogueDescription"),
                PrimaryButtonText = ResourceLoader.GetForCurrentView().GetString("changesCharactersPageDialogueApplyChanges"),
                SecondaryButtonText = ResourceLoader.GetForCurrentView().GetString("changesCharactersPageDialogueDontApplyChanges"),
                CloseButtonText = ResourceLoader.GetForCurrentView().GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = MainPage.Current.RequestedTheme,
            };
            AppView.currentlyOpenedDialogue = leaveDialog;
            leaveDialog.RequestedTheme = AppView.current.ActualTheme;

            ContentDialogResult result = await leaveDialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    CharactersPage.current.ApplyChanges();

                    AppView.current.GoBack();
                    break;
                case ContentDialogResult.Secondary:
                    CharactersPage.current.CancelEdit();

                    AppView.current.GoBack();
                    break;
            }
            AppView.currentlyOpenedDialogue = null;
        }

        public static async Task DisplayNoCharactersInProjectDialogue()
        {
            CheckForOpenDialogueAndClose();

            ContentDialog noCharactersDialog = new ContentDialog
            {
                Title = ResourceLoader.GetForCurrentView().GetString("noCharactersDialogueTitle"),
                Content = ResourceLoader.GetForCurrentView().GetString("noCharactersDialogueDescription"),
                PrimaryButtonText = ResourceLoader.GetForCurrentView().GetString("noCharactersDialogueAddNew"),
                CloseButtonText = ResourceLoader.GetForCurrentView().GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = MainPage.Current.RequestedTheme,
            };
            AppView.currentlyOpenedDialogue = noCharactersDialog;
            noCharactersDialog.RequestedTheme = AppView.current.ActualTheme;

            ContentDialogResult result = await noCharactersDialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    AppView.current.ChangePage(AppView.Pages.Characters);
                    CharactersPage.current.Add();
                    break;
            }
            AppView.currentlyOpenedDialogue = null;
        }
    }
}
