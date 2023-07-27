using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Storylines
{
    public sealed partial class AppView : Page
    {
        public static AppView current { get; private set; }

        public static ContentDialog currentlyOpenedDialogue;

        public AppView()
        {
            InitializeComponent();
            current = this;

            UpdateTitleBar();

            ChangePage(Pages.MainPage);

            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;

            if (SettingsValues.autosaveEnabled)
                Autosave.Enable();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown; ;
            Loaded += delegate { _ = Focus(FocusState.Programmatic); };
        }

        public void UpdateTitleBar()
        {
            if (page == Pages.Settings)
            {
                var version = Package.Current.Id.Version;
                appHeader.Text =
                $"{Package.Current.DisplayName}" +
                $" {version.Major}.{version.Minor}{(version.Build.ToString().Equals("0") ? string.Empty : $".{version.Build}")}{(version.Revision.ToString().Equals("0") ? string.Empty : $".{version.Revision}")}" +
                $"{(Package.Current.IsDevelopmentMode ? " Dev" : " Preview")}";
            }
            else
            {
                var name = GetName();
                if (name == null)
                    appHeader.Text = Package.Current.DisplayName;
                else
                    appHeader.Text = name;
            }

            appHeaderSave.Text = $" {ResourceLoader.GetForCurrentView().GetString("appHeaderEdited")}";
            appHeaderSave.Visibility = TimeTravelSystem.unSavedProgress ? Visibility.Visible : Visibility.Collapsed;
            //$"{_ = (TimeTravelSystem.unSavedProgress ? "*" : string.Empty)} -";
        }

        public string GetName()
        {
            if (SaveSystem.currentProject != null)
                if (!string.IsNullOrEmpty(SaveSystem.currentProject.projectName))
                    return SaveSystem.currentProject.projectName;
                else
                    return SaveSystem.currentProject.name;
            else
                return null;
        }

        public void ClearEverything()
        {
            MainPage.ChapterText.textBox.Document.SetText(Windows.UI.Text.TextSetOptions.None, "");
            Chapter.chapters.Clear();
            Character.characters.Clear();
            MainPage.Current.EnableOrDisableChapterTools(false);
        }

        public void UsingWindows10()
        {
            if (/*!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.AcrylicBrush") ||*/
                SettingsValues.IsCurrentVersionGreater($"{SystemInformation.Instance.OperatingSystemVersion.Major}.{SystemInformation.Instance.OperatingSystemVersion.Minor}.{SystemInformation.Instance.OperatingSystemVersion.Build}.{SystemInformation.Instance.OperatingSystemVersion.Revision}", "10.0.22000.0"))
            {
                Background = new SolidColorBrush(Colors.Transparent);
                BackdropMaterial.SetApplyToRootOrPageBackground(current, true);
            }
            else
            {
                BackdropMaterial.SetApplyToRootOrPageBackground(current, false);
                LoadProjectDialogue.osMargin = new Thickness(-10, 4, -20, 4);
                LoadProjectDialogue.osWidth = 374;
            }
        }

        #region Review and Notifications
        private void OnRateNowButton_Click(object sender, RoutedEventArgs e)
        {
            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_Review("Review infoBar", "Rate now");

            reviewRequestInfoBar.Visibility = Visibility.Collapsed;
            reviewRequestInfoBar.IsOpen = false;
            _ = MicrosoftStoreAndAppCenterFunctions.PromptUserToRateApp();
        }

        private void OnRateNotNow_Click(object sender, RoutedEventArgs e)
        {
            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_Review("Review infoBar", "Not now");
            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] = (int)SettingsValues.ReviewPrompt.NotYet;
            reviewRequestInfoBar.Visibility = Visibility.Collapsed;
            reviewRequestInfoBar.IsOpen = false;
            NotificationManager.ClearBadgeNotification();
        }

        private void OnRateNeverShowAgain_Click(object sender, RoutedEventArgs e)
        {
            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_Review("Review infoBar", "Never show again");
            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] = (int)SettingsValues.ReviewPrompt.NeverShowAgain;
            reviewRequestInfoBar.Visibility = Visibility.Collapsed;
            reviewRequestInfoBar.IsOpen = false;
            NotificationManager.ClearBadgeNotification();
        }

        private void OnRateNotNow_CloseButtonClick(InfoBar sender, object args)
        {
            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_Review("Review infoBar", "Not now");
            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] = (int)SettingsValues.ReviewPrompt.NotYet;
            reviewRequestInfoBar.Visibility = Visibility.Collapsed;
            reviewRequestInfoBar.IsOpen = false;
            NotificationManager.ClearBadgeNotification();
        }
       
        private void OnAlertNotificationInfoBar_CloseButtonClick(InfoBar sender, object args)
        {
            NotificationManager.InAppNotification_Close();
            AppView.current.alertNotificationInfoBar.Visibility = Visibility.Collapsed;
        }

        private void OnUpdateAvailableInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            NotificationManager.NewUpdateAvailable_Close();
        }
        #endregion

        #region Pages
        public enum Pages { Settings, Characters, MainPage }
        public Pages page;

        public void ChangePage(Pages currentPage)
        {
            current.backButton.Visibility = Visibility.Visible;

            switch (currentPage)
            {
                case Pages.Settings:
                    pagesView.Navigate(typeof(SettingsPage));
                    break;

                case Pages.Characters:
                    pagesView.Navigate(typeof(CharactersPage), null, new DrillInNavigationTransitionInfo());
                    break;

                case Pages.MainPage:
                    pagesView.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
                    break;
            }

            page = currentPage;

            UpdateTitleBar();
            BackButtonCheck();
        }

        public void BackButtonCheck()
        {
            if (pagesView.CanGoBack || MainPage.ReadMode != null || MainPage.FocusMode != null)
                backButton.Visibility = Visibility.Visible;
            else
                backButton.Visibility = Visibility.Collapsed;
        }

        public void GoBack()
        {
            if (pagesView.CanGoBack)
            {
                if (CharactersPage.current != null && CharactersPage.current.unappliedChanges)
                {
                    _ = NotificationManager.DisplayNotAppliedChangesCharactersPageDialogue(false);
                    return;
                }

                pagesView.GoBack(new DrillInNavigationTransitionInfo());
            }
            else
            if (MainPage.FocusMode != null)
            {
                if (MainPage.FocusMode.final)
                    MainPage.FocusMode.Leave();
                else
                    _ = NotificationManager.DisplayNotFinishedInFocusModeDialogue();
                MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_FocusMode_Leave(MainPage.FocusMode.final);
            }
            else
            if (MainPage.ReadMode != null)
                MainPage.ReadMode.Leave();

            UpdateTitleBar();
            BackButtonCheck();
        }

        private void OnBackButton_Click(object sender, RoutedEventArgs e)
        {
            GoBack();
        }

        private void System_BackRequested(object sender, BackRequestedEventArgs e)
        {
            OnBackButton_Click(sender, new RoutedEventArgs());
        }
        #endregion

        private void OnShortCut_Pressed(object sender, KeyRoutedEventArgs e)
        {
            //ShortcutManager.Check(e);
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            ShortcutManager.Check(e);
        }
    }
}
