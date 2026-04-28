using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Views.Pages;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Models;
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
using Storylines.Services.Interfaces;

namespace Storylines
{
    public sealed partial class AppView : Page
    {
        public static AppView current { get; private set; }

        public static ContentDialog currentlyOpenedDialogue;

        private readonly AppViewModel _viewModel;
        private readonly EventAggregator _events;
        private readonly INavigationService _navigation;
        private readonly ProjectState _projectState;
        private readonly ITextEditorService _textEditor;

        public AppViewModel ViewModel => _viewModel;

        public AppView()
        {
            InitializeComponent();
            current = this;

            _viewModel = App.GetService<AppViewModel>();
            _events = App.GetService<EventAggregator>();
            _navigation = App.GetService<INavigationService>();
            _projectState = App.GetService<ProjectState>();
            _textEditor = App.GetService<ITextEditorService>();

            // Wire NavigationService to the Frame
            _navigation.Initialize(pagesView);

            ViewModel.UpdateTitleBar();

            ChangePage(Pages.MainPage);

            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;

            // Subscribe to tools state changes (published by SaveSystem)
            _events.Subscribe<ToolsStateChangedEvent>(e =>
            {
                if (MainPage.Current != null)
                    MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(e.IsStorylinesDocument);
            });

            if (SettingsValues.autosaveEnabled)
                AutosaveService.Enable();

            RecoveryService.Start();

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
            _textEditor.Clear();
            _projectState.Clear();
            _events.Publish(new ChapterToolsStateEvent { Enabled = false });
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
            App.TryGetService<Storylines.Services.Interfaces.ITelemetryService>()?.TrackReviewInteraction("review_infobar", "rate_now");

            reviewRequestInfoBar.Visibility = Visibility.Collapsed;
            reviewRequestInfoBar.IsOpen = false;
            _ = MicrosoftStoreFunctions.PromptUserToRateAppAsync("review_infobar");
        }

        private void OnRateNotNow_Click(object sender, RoutedEventArgs e)
        {
            App.TryGetService<Storylines.Services.Interfaces.ITelemetryService>()?.TrackReviewInteraction("review_infobar", "not_now");
            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] = (int)SettingsValues.ReviewPrompt.NotYet;
            reviewRequestInfoBar.Visibility = Visibility.Collapsed;
            reviewRequestInfoBar.IsOpen = false;
            NotificationManager.ClearBadgeNotification();
        }

        private void OnRateNeverShowAgain_Click(object sender, RoutedEventArgs e)
        {
            App.TryGetService<Storylines.Services.Interfaces.ITelemetryService>()?.TrackReviewInteraction("review_infobar", "never_show_again");
            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] = (int)SettingsValues.ReviewPrompt.NeverShowAgain;
            reviewRequestInfoBar.Visibility = Visibility.Collapsed;
            reviewRequestInfoBar.IsOpen = false;
            NotificationManager.ClearBadgeNotification();
        }

        private void OnRateNotNow_CloseButtonClick(InfoBar sender, object args)
        {
            App.TryGetService<Storylines.Services.Interfaces.ITelemetryService>()?.TrackReviewInteraction("review_infobar", "dismissed");
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
        public enum Pages { Settings, Characters, MainPage, BranchingDialogue }
        public Pages page;

        public void ChangePage(Pages currentPage)
        {
            current.backButton.Visibility = Visibility.Visible;

            // Use NavigationService for consistent navigation
            switch (currentPage)
            {
                case Pages.Settings:
                    _navigation.NavigateTo(NavigationTarget.Settings);
                    break;
                case Pages.Characters:
                    _navigation.NavigateTo(NavigationTarget.Characters);
                    break;
                case Pages.MainPage:
                    _navigation.NavigateTo(NavigationTarget.MainPage);
                    break;
                case Pages.BranchingDialogue:
                    _navigation.NavigateTo(NavigationTarget.BranchingDialogue);
                    break;
            }

            page = currentPage;

            UpdateTitleBar();
            BackButtonCheck();
        }

        public void BackButtonCheck()
        {
            var modeService = App.TryGetService<Storylines.Services.Modes.EditorModeService>();
            bool hasModeActive = modeService?.Current.Id != "edit";
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
            {
                var modeService = App.TryGetService<Storylines.Services.Modes.EditorModeService>();
                if (modeService != null && modeService.Current.Id != "edit")
                {
                    bool wasFinal = modeService.Current.CanLeave;
                    if (wasFinal)
                    {
                        App.TryGetService<Storylines.Services.Interfaces.ITelemetryService>()?.TrackFocusModeLeft(true);
                        modeService.Deactivate();
                    }
                    else
                    {
                        App.TryGetService<Storylines.Services.Interfaces.ITelemetryService>()?.TrackFocusModeLeft(false);
                        _ = NotificationManager.DisplayNotFinishedInFocusModeDialogue();
                    }
                }
            }

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

            if (TimeTravelSystem.unSavedProgress)
                _ = NotificationManager.DisplayUnsavedProgressDialogue(false);
            else
                SaveSystem.DefaultLaunch(file);
        }
        #endregion
    }
}
