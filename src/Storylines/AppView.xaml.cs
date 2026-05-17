using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Views.Pages;
using Windows.UI;
using Microsoft.UI.Xaml.Media.Animation;

namespace Storylines;

    public sealed partial class AppView : Page
    {

        private readonly WindowContext _windowContext;
        private readonly AppViewModel _viewModel;
        private readonly EventAggregator _events;
        private readonly INavigationService _navigation;
        private readonly IProjectPersistenceService _persistence;
        private readonly ProjectState _projectState;
        private readonly ITextEditorService _textEditor;

        public AppViewModel ViewModel => _viewModel;

        public AppView()
        {
            _windowContext = App.GetService<WindowContext>();
            _viewModel = App.GetService<AppViewModel>();
            _events = App.GetService<EventAggregator>();
            _navigation = App.GetService<INavigationService>();
            _persistence = App.GetService<IProjectPersistenceService>();
            _projectState = App.GetService<ProjectState>();
            _textEditor = App.GetService<ITextEditorService>();

            InitializeComponent();

            _windowContext.AppView = this;
            App.GetService<IWindowManager>().SetCurrent(_windowContext);

            // Wire NavigationService to the Frame
            _navigation.Initialize(pagesView);
            _navigation.Navigated += OnNavigationTargetChanged;

            ViewModel.UpdateTitleBar();

            ChangePage(Pages.MainPage);

            // Subscribe to back navigation via AppWindow (no SystemNavigationManager in WinUI 3)

            // Subscribe to tools state changes published by the persistence service.
            // All handlers are dispatched to the UI thread because Publish can be called
            // from background threads (e.g. DictationService, autosave, recovery).
            _events.Subscribe<ToolsStateChangedEvent>(e =>
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (_windowContext.MainPage is not null)
                        _windowContext.MainPage.UpdateToolsForDocument(e.IsStorylinesDocument);
                }));

            // Route INotificationService events to UI — decouples service from AppView.current refs.
            _events.Subscribe<InAppNotificationEvent>(e =>
                DispatcherQueue.TryEnqueue(() =>
                    DisplayInAppNotification(e.Severity, e.Title, e.Message, e.Duration)));

            _events.Subscribe<PersistentNotificationEvent>(e =>
                DispatcherQueue.TryEnqueue(() =>
                    notificationHost.ShowPersistentNotification(e)));

            _events.Subscribe<UpdatePersistentProgressEvent>(e =>
                DispatcherQueue.TryEnqueue(() =>
                    notificationHost.UpdatePersistentProgress(e.Value, e.IsIndeterminate)));

            _events.Subscribe<DismissPersistentNotificationEvent>(_ =>
                DispatcherQueue.TryEnqueue(() =>
                    notificationHost.DismissPersistentNotification()));

            _events.Subscribe<ProgressBarEvent>(e =>
                DispatcherQueue.TryEnqueue(() =>
                {
                    var mainPage = _windowContext?.MainPage;
                    if (mainPage?.mainProgressBar is null) return;

                    if (!e.Show)
                    {
                        mainPage.mainProgressBar.Visibility = Visibility.Collapsed;
                        return;
                    }

                    mainPage.mainProgressBar.Visibility = Visibility.Visible;
                    mainPage.mainProgressBar.IsIndeterminate = e.IsIndeterminate;
                    mainPage.mainProgressBar.Value = e.Value;
                    mainPage.mainProgressBar.ShowPaused = e.State == ProgressBarEvent.ProgressState.Paused;
                    mainPage.mainProgressBar.ShowError = e.State == ProgressBarEvent.ProgressState.Error;
                }));

            if (SettingsValues.autosaveEnabled)
                _persistence.EnableAutosave();

            RecoveryService.Start();

            _windowContext.RootElement.KeyDown += Window_KeyDown;
            Loaded += delegate { _ = Focus(FocusState.Programmatic); };
        }

        public void UpdateTitleBar()
        {
            ViewModel.CurrentPage = (AppViewModel.AppPages)(int)page;
            ViewModel.UpdateTitleBar();
        }

        private void DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, string text, string message, TimeSpan? duration)
        {
            notificationHost.ShowNotification(
                severity,
                text,
                message,
                duration ?? TimeSpan.FromSeconds(Constants.LayoutConstants.NotificationDismissSeconds));
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
            OpenGlobalSearch(titleBarSearchBox.Text);
        }

        private void OnFocusModeAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var modeSvc = App.TryGetService<Services.Modes.EditorModeService>();
            if (modeSvc is null) return;
            // F11: toggle — leave focus if active, otherwise open mode picker
            if (modeSvc.IsInMode("focus"))
                TryExitActiveMode();
            else
                App.GetService<Services.Interfaces.IDialogService>().OpenFocusMode();
        }

        private void OnReadAloudAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var speechHub = App.GetService<SpeechHubViewModel>();
            if (speechHub.StartReadAloudCommand.CanExecute(null))
                speechHub.StartReadAloudCommand.Execute(null);
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

        #region Pages
        public enum Pages { Settings, Characters, MainPage,
#if PRIVATE_PLUGINS
            BranchingDialogue,
#endif
        }
        public Pages page;

        public void ChangePage(Pages currentPage)
        {
            App.GetService<IWindowManager>().SetCurrent(_windowContext);

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
#if PRIVATE_PLUGINS
                case Pages.BranchingDialogue:
                    _navigation.NavigateTo(NavigationTarget.BranchingDialogue);
                    break;
#endif
            }
        }

        public void BackButtonCheck()
        {
            var modeService = App.TryGetService<Storylines.Services.Modes.EditorModeService>();
            bool hasModeActive = modeService?.Current is not null && modeService.Current.Id != "edit";
            ViewModel.UpdateBackButtonState(_navigation.CanGoBack, hasModeActive);
        }

        public void GoBack()
        {
            if (_navigation.CanGoBack)
            {
                if (_windowContext.CharactersPage is not null && _windowContext.CharactersPage.unappliedChanges)
                {
                    _ = App.GetService<IDialogService>().ShowUnappliedCharacterChangesDialogueAsync();
                    return;
                }

                _navigation.GoBack();
                return;
            }
            else
            {
                TryExitActiveMode();
            }

            UpdateTitleBar();
            BackButtonCheck();
        }

        public bool TryExitActiveMode()
        {
            var modeService = App.TryGetService<Storylines.Services.Modes.EditorModeService>();
            if (modeService is null || modeService.Current.Id == "edit")
                return true;

            bool isFocusMode = modeService.IsInMode("focus");
            if (modeService.Current.CanLeave)
            {
                if (isFocusMode)
                    App.TryGetService<Storylines.Services.Interfaces.ITelemetryService>()?.TrackFocusModeLeft(true);

                modeService.Deactivate();
                UpdateTitleBar();
                BackButtonCheck();
                return true;
            }

            if (isFocusMode)
                App.TryGetService<Storylines.Services.Interfaces.ITelemetryService>()?.TrackFocusModeLeft(false);

            _ = App.GetService<IDialogService>().ShowFocusModeLeaveDialogueAsync();
            return false;
        }

        private void OnNavigationTargetChanged(NavigationTarget target)
        {
            page = target switch
            {
                NavigationTarget.Settings => Pages.Settings,
                NavigationTarget.Characters => Pages.Characters,
#if PRIVATE_PLUGINS
                NavigationTarget.BranchingDialogue => Pages.BranchingDialogue,
#endif
                _ => Pages.MainPage,
            };

            UpdateTitleBar();
            BackButtonCheck();
        }

        private void OnGlobalSearchButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGlobalSearch(titleBarSearchBox.Text);
        }

        private void OnTitleBarSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            OpenGlobalSearch(args.QueryText);
        }

        private void OnAppTitleBar_BackRequested(TitleBar sender, object args)
        {
            GoBack();
        }

        private void OpenGlobalSearch(string initialQuery)
        {
            _ = GlobalSearchDialogue.OpenAsync(initialQuery);
            titleBarSearchBox.Text = string.Empty;
        }
        #endregion

        private void Window_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            App.GetService<IWindowManager>().SetCurrent(_windowContext);
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
                _ = App.GetService<IDialogService>().ShowUnsavedProgressDialogueAsync(false);
            else
                _persistence.DefaultLaunch(file);
        }
        #endregion
    }
