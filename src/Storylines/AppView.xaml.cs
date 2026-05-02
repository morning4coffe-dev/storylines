using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Views.Pages;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Models;
using Storylines.ViewModels;
using System;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
        private readonly IProjectPersistenceService _persistence;
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
            _persistence = App.GetService<IProjectPersistenceService>();
            _projectState = App.GetService<ProjectState>();
            _textEditor = App.GetService<ITextEditorService>();

            // Wire NavigationService to the Frame
            _navigation.Initialize(pagesView);

            ViewModel.UpdateTitleBar();

            ChangePage(Pages.MainPage);

            // Subscribe to back navigation via AppWindow (no SystemNavigationManager in WinUI 3)

            // Subscribe to tools state changes published by the persistence service.
            _events.Subscribe<ToolsStateChangedEvent>(e =>
            {
                if (MainPage.Current != null)
                    MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(e.IsStorylinesDocument);
            });

            // Route INotificationService events to UI — decouples service from AppView.current refs.
            _events.Subscribe<InAppNotificationEvent>(e =>
                NotificationManager.DisplayInAppNotification(e.Severity, e.Title, e.LongText));

            _events.Subscribe<ProgressBarEvent>(e =>
            {
                if (!e.Show)
                {
                    NotificationManager.HideMainProgressBar();
                    return;
                }
                NotificationManager.DisplayMainProgressBar(e.IsIndeterminate);
                if (e.Value > 0)
                    NotificationManager.UpdateMainProgressBar(
                        e.Value,
                        (NotificationManager.ProgressState)(int)e.State);
            });

            if (SettingsValues.autosaveEnabled)
                _persistence.EnableAutosave();

            RecoveryService.Start();

            App.MainWindow.Content.KeyDown += Window_KeyDown;
            Loaded += delegate { _ = Focus(FocusState.Programmatic); };
        }

        public void UpdateTitleBar()
        {
            ViewModel.CurrentPage = (AppViewModel.AppPages)(int)page;
            ViewModel.UpdateTitleBar();
        }

        // ── Global keyboard accelerator handlers ──────────────────────────────

        private void OnSaveAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            _persistence.Save();
        }

        private void OnSaveCopyAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            _persistence.SaveCopy();
        }

        private void OnUndoAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var undoSvc = App.GetService<Services.Interfaces.IUndoRedoService>();
            string context = page == Pages.Characters ? "characters" : "chapters";
            if (undoSvc.CanUndo(context))
                undoSvc.Undo(context);
        }

        private void OnRedoAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var undoSvc = App.GetService<Services.Interfaces.IUndoRedoService>();
            string context = page == Pages.Characters ? "characters" : "chapters";
            if (undoSvc.CanRedo(context))
                undoSvc.Redo(context);
        }

        private void OnCommandPaletteAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            // Placeholder: will open command palette overlay when Phase 6 UI ships.
        }

        private void OnFocusModeAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var modeSvc = App.TryGetService<Services.Modes.EditorModeService>();
            if (modeSvc == null) return;
            // F11: toggle — leave focus if active, otherwise open mode picker
            if (modeSvc.IsInMode("focus"))
                modeSvc.TryLeave();
            else
                App.GetService<Services.Interfaces.IDialogService>().OpenFocusMode();
        }

        private void OnReadAloudAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            Views.Pages.MainPage.CommandBar.ReadAloud();
        }

        private void OnDictationAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            App.GetService<SpeechHubViewModel>().ToggleDictationCommand.Execute(null);
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
            // In WinUI 3, Mica/Acrylic backdrop is set on the Window via SystemBackdrop.
            // No need for BackdropMaterial attached property.
            Background = new SolidColorBrush(Colors.Transparent);
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

        private void OnUpdateAvailablePrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            _ = MicrosoftStoreFunctions.InstallAvailableUpdatesAsync();
        }

        private void OnUpdateAvailableSecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            NotificationManager.NewUpdateAvailable_Close();
        }

        private void OnUpdateAvailableInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            NotificationManager.NewUpdateAvailable_Close();
        }
        #endregion

        #region Pages
        public enum Pages { Settings, Characters, MainPage,
#if PRIVATE_PLUGINS
            BranchingDialogue,
#endif
        }
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

        private void System_BackRequested(object sender, RoutedEventArgs e)
        {
            OnBackButton_Click(sender, new RoutedEventArgs());
        }
        #endregion

        private void Window_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            ShortcutManager.Check(e);
        }

        #region Drag and Drop
        private async void OnGrid_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
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

        private async void OnGrid_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
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
                _persistence.DefaultLaunch(file);
        }
        #endregion
    }
}
