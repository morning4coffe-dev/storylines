using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using Storylines.ViewModels;
using System;
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

        public AppViewModel ViewModel => ServiceLocator.AppViewModel;

        public AppView()
        {
            InitializeComponent();
            current = this;

            // Wire NavigationService to the Frame
            ServiceLocator.InitializeNavigation(pagesView);

            ViewModel.UpdateTitleBar();

            ChangePage(Pages.MainPage);

            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;

            // Subscribe to tools state changes (published by SaveSystem)
            ServiceLocator.Events.Subscribe<ToolsStateChangedEvent>(e =>
            {
                if (MainPage.Current != null)
                    MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(e.IsStorylinesDocument);
            });

            if (SettingsValues.autosaveEnabled)
                Autosave.Enable();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Loaded += delegate { _ = Focus(FocusState.Programmatic); };
        }

        public void UpdateTitleBar()
        {
            ViewModel.CurrentPage = (AppViewModel.AppPages)(int)page;
            ViewModel.UpdateTitleBar();
        }

        public string GetName()
        {
            return ViewModel.GetProjectName();
        }

        public void ClearEverything()
        {
            ServiceLocator.TextEditor.Clear();
            ServiceLocator.ProjectState.Clear();
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

            // Use NavigationService for consistent navigation
            switch (currentPage)
            {
                case Pages.Settings:
                    ServiceLocator.Navigation.NavigateTo(Scripts.Services.Interfaces.NavigationTarget.Settings);
                    break;
                case Pages.Characters:
                    ServiceLocator.Navigation.NavigateTo(Scripts.Services.Interfaces.NavigationTarget.Characters);
                    break;
                case Pages.MainPage:
                    ServiceLocator.Navigation.NavigateTo(Scripts.Services.Interfaces.NavigationTarget.MainPage);
                    break;
            }

            page = currentPage;

            UpdateTitleBar();
            BackButtonCheck();
        }

        public void BackButtonCheck()
        {
            bool hasModeActive = MainPage.ReadMode != null || MainPage.FocusMode != null;
            ViewModel.UpdateBackButtonVisibility(pagesView.CanGoBack, hasModeActive);
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

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            ShortcutManager.Check(e);
        }

        #region Drag and Drop
        private async void OnGrid_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            if (!e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
                return;

            var deferral = e.GetDeferral();
            try
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count == 1 && items[0] is StorageFile file &&
                    (file.FileType == ".srl" || file.FileType == ".txt"))
                {
                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                    e.DragUIOverride.Caption = "Open in Storylines";
                    e.DragUIOverride.IsGlyphVisible = true;
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnGrid_Drop(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            if (!e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
                return;

            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count != 1 || !(items[0] is StorageFile file) ||
                (file.FileType != ".srl" && file.FileType != ".txt"))
                return;

            if (Scripts.Functions.TimeTravelSystem.unSavedProgress)
                _ = Scripts.Functions.NotificationManager.DisplayUnsavedProgressDialogue(false);
            else
                SaveSystem.DefaultLaunch(file);
        }
        #endregion
    }
}
