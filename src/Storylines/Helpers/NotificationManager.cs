using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Views.Pages;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Storylines.Helpers
{
    class NotificationManager
    {
        public enum InAppNotificationType { None, NewUpdate, Review, ThankYou };
        public static InAppNotificationType currentInAppNotification = InAppNotificationType.None;

        private static IDialogService Dialogs => App.GetService<IDialogService>();
        private static IProjectPersistenceService Persistence => App.GetService<IProjectPersistenceService>();
        private static WindowContext WindowContext => App.GetService<WindowContext>();
        private static AppView Shell => WindowContext.AppView ?? AppView.current;
        private static MainPage Main => WindowContext.MainPage ?? MainPage.Current;

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
            Main.mainProgressBar.Visibility = Visibility.Visible;
            Main.mainProgressBar.IsIndeterminate = isIndeterminate;

            UpdateMainProgressBar(0, ProgressState.Normal);
        }

        public enum ProgressState { Normal, Paused, Error };
        public static void UpdateMainProgressBar(int value, ProgressState state)
        {
            Main.mainProgressBar.Value = value;

            switch (state)
            {
                case ProgressState.Paused:
                    Main.mainProgressBar.ShowPaused = true;
                    Main.mainProgressBar.ShowError = false;
                    break;
                case ProgressState.Error:
                    Main.mainProgressBar.ShowPaused = false;
                    Main.mainProgressBar.ShowError = true;
                    break;
                default:
                    Main.mainProgressBar.ShowPaused = false;
                    Main.mainProgressBar.ShowError = false;
                    break;
            }
        }

        public static void HideMainProgressBar()
        {
            Main.mainProgressBar.Visibility = Visibility.Collapsed;
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

        public static void DisplayReviewPrompt()
        {
            App.TryGetService<ITelemetryService>()?.TrackReviewPromptDisplayed("review_timer");
            Shell.reviewRequestInfoBar.IsOpen = true;
            Shell.reviewRequestInfoBar.Visibility = Visibility.Visible;
            Shell.reviewRequestInfoBar.RequestedTheme = Shell.ActualTheme;

            DisplayBadgeNotification("attention");
        }

        public static void DisplayThankYou()
        {
            Shell.reviewRequestThankYouInfoBar.IsOpen = true;
            Shell.reviewRequestThankYouInfoBar.Visibility = Visibility.Visible;
            Shell.reviewRequestThankYouInfoBar.RequestedTheme = Shell.ActualTheme;
        }

        private static DispatcherTimer InAppNotificationTimer;

        public static void DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, string text, string longText)
        {
            Shell.alertNotificationInfoBar.IsOpen = true;
            Shell.alertNotificationInfoBar.Visibility = Visibility.Visible;

            Shell.alertNotificationInfoBar.Severity = severity;
            Shell.alertNotificationInfoBar.Title = text;

            Shell.alertNotificationInfoBar.RequestedTheme = Shell.ActualTheme;

            if (longText.Length < 1)
                Shell.alertNotificationInfoBarTextStack.Visibility = Visibility.Collapsed;
            else
            {
                Shell.alertNotificationInfoBarTextStack.Visibility = Visibility.Visible;
                Shell.alertNotificationInfoBarText.Text = longText;
            }

            if (InAppNotificationTimer != null)
            {
                InAppNotificationTimer.Tick -= InAppNotificationTimer_Tick;
                InAppNotificationTimer.Stop();
                InAppNotificationTimer = null;
            }

            InAppNotificationTimer = new DispatcherTimer();
            InAppNotificationTimer.Tick += InAppNotificationTimer_Tick;
            InAppNotificationTimer.Interval = TimeSpan.FromSeconds(Constants.LayoutConstants.NotificationDismissSeconds);
            InAppNotificationTimer.Start();

            DisplayBadgeNotification("attention");
        }

        private static void InAppNotificationTimer_Tick(object sender, object e)
        {
            (sender as DispatcherTimer).Stop();

            if (InAppNotificationTimer != null)
            {
                Shell.alertNotificationInfoBar.Visibility = Visibility.Collapsed;
                Shell.alertNotificationInfoBar.IsOpen = false;

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

        public static async Task DisplayUnsavedProgressDialogue(bool appClosing)
        {
            ContentDialogResult result = await Dialogs.ShowMessageAsync(new DialogDefinition
            {
                Title = ResourceLoader.GetForViewIndependentUse().GetString("exitWithoutSaveDialogTitle"),
                Content = ResourceLoader.GetForViewIndependentUse().GetString("exitWithoutSaveDialogDescription"),
                PrimaryButtonText = ResourceLoader.GetForViewIndependentUse().GetString("exitWithoutSaveDialogSave"),
                SecondaryButtonText = ResourceLoader.GetForViewIndependentUse().GetString("exitWithoutSaveDialogDontSave"),
                CloseButtonText = ResourceLoader.GetForViewIndependentUse().GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
            });

            switch (result)
            {
                case ContentDialogResult.Primary:
                    Persistence.SaveAndExitOrClearAll(appClosing);
                    break;
                case ContentDialogResult.Secondary:
                    await RecoveryService.ClearRecoveryDataAsync();

                    if (appClosing)
                    {
                        App.GetService<IUndoRedoService>().MarkClean();
                        App.GetService<IWindowManager>().Close(WindowContext);
                    }
                    else
                    {
                        if (Persistence.CurrentProject != null)
                            Persistence.CurrentProject.file = null;
                        Shell.ClearEverything();
                        TimeTravelSystem.unSavedProgress = false;

                        Dialogs.OpenLoadDialogue();
                    }
                    break;
            }
        }

        public static async Task DisplayNotFinishedInFocusModeDialogue()
        {
            ContentDialogResult result = await Dialogs.ShowMessageAsync(new DialogDefinition
            {
                Title = ResourceLoader.GetForViewIndependentUse().GetString("FocusModeLeaveDialogueTitle"),
                Content = ResourceLoader.GetForViewIndependentUse().GetString("FocusModeLeaveDialogueDescription"),
                PrimaryButtonText = ResourceLoader.GetForViewIndependentUse().GetString("FocusModeLeaveDialogueStay"),
                SecondaryButtonText = ResourceLoader.GetForViewIndependentUse().GetString("FocusModeLeaveDialogueLeave"),
                DefaultButton = ContentDialogButton.Primary,
            });

            switch (result)
            {
                case ContentDialogResult.Secondary:
                    App.TryGetService<Storylines.Services.Modes.EditorModeService>()?.Deactivate();
                    break;
            }
        }

        public static async Task DisplayNotAppliedChangesCharactersPageDialogue(bool appClosing)
        {
            ContentDialogResult result = await Dialogs.ShowMessageAsync(new DialogDefinition
            {
                Title = ResourceLoader.GetForViewIndependentUse().GetString("changesCharactersPageDialogueTitle"),
                Content = ResourceLoader.GetForViewIndependentUse().GetString("changesCharactersPageDialogueDescription"),
                PrimaryButtonText = ResourceLoader.GetForViewIndependentUse().GetString("changesCharactersPageDialogueApplyChanges"),
                SecondaryButtonText = ResourceLoader.GetForViewIndependentUse().GetString("changesCharactersPageDialogueDontApplyChanges"),
                CloseButtonText = ResourceLoader.GetForViewIndependentUse().GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
            });

            switch (result)
            {
                case ContentDialogResult.Primary:
                    WindowContext.CharactersPage.ApplyChanges();

                    Shell.GoBack();
                    break;
                case ContentDialogResult.Secondary:
                    WindowContext.CharactersPage.CancelEdit();

                    Shell.GoBack();
                    break;
            }
        }

        public static async Task DisplayNoCharactersInProjectDialogue()
        {
            ContentDialogResult result = await Dialogs.ShowMessageAsync(new DialogDefinition
            {
                Title = ResourceLoader.GetForViewIndependentUse().GetString("noCharactersDialogueTitle"),
                Content = ResourceLoader.GetForViewIndependentUse().GetString("noCharactersDialogueDescription"),
                PrimaryButtonText = ResourceLoader.GetForViewIndependentUse().GetString("noCharactersDialogueAddNew"),
                CloseButtonText = ResourceLoader.GetForViewIndependentUse().GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
            });

            switch (result)
            {
                case ContentDialogResult.Primary:
                    Shell.ChangePage(AppView.Pages.Characters);
                    WindowContext.CharactersPage.Add();
                    break;
            }
        }
    }
}
