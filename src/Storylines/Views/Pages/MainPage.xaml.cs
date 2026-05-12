using Storylines.Views.Controls;
using Storylines.Services.Modes;

namespace Storylines.Views.Pages;

/// <summary>
/// Main editor page for the Storylines application.
/// Manages UI layout, window context setup, and editor interactions.
/// Most business logic is delegated to the ViewModel.
/// </summary>
public sealed partial class MainPage : Page
{
    private static IProjectPersistenceService Persistence => App.GetService<IProjectPersistenceService>();
    private static IPreferencesService Preferences => App.GetService<IPreferencesService>();

    private ChaptersList ChapterList => _windowContext.ChapterList;
    private MainCommandBar CommandBar => _windowContext.CommandBar;
    private ChapterTextBox ChapterText => _windowContext.ChapterText;

    private readonly EventAggregator _events;
    private readonly MainPageViewModel _viewModel;
    private readonly EditorModeService _modeService;
    private readonly WindowContext _windowContext;

    private string _pendingChapterToken;
    private DispatcherTimer _sessionTimer;

    public MainPageViewModel ViewModel => _viewModel;

    public MainPage()
    {
        _windowContext = App.GetService<WindowContext>();
        _events = App.GetService<EventAggregator>();
        _viewModel = App.GetService<MainPageViewModel>();
        _modeService = App.GetService<EditorModeService>();

        InitializeComponent();

        _windowContext.MainPage = this;
        App.GetService<IWindowManager>().SetCurrent(_windowContext);
        _windowContext.AppView.page = AppView.Pages.MainPage;

        // Subscribe to UI-level events that require direct control manipulation
        _events.Subscribe<ChapterToolsStateEvent>(OnChapterToolsStateChanged);
        _events.Subscribe<ToggleChapterListEvent>(OnToggleChapterList);
        _events.Subscribe<RefreshNotesPaneEvent>(_ => RefreshNotesPane());
        _events.Subscribe<SettingChangedEvent>(OnSettingChanged);
        _modeService.ModeChanged += OnModeSurfaceLayoutChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        OnModeSurfaceLayoutChanged(_modeService.Current);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _pendingChapterToken = e.Parameter as string;
        TrySelectPendingChapter();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Handle pending file activation
        var pendingActivatedItem = _windowContext.PendingActivatedItem ?? App.PendingActivatedItem;
        if (pendingActivatedItem is not null)
        {
            Persistence.DefaultLaunch(pendingActivatedItem);
            _windowContext.PendingActivatedItem = null;
            if (ReferenceEquals(App.PendingActivatedItem, pendingActivatedItem))
                App.PendingActivatedItem = null;
        }

        // Restore chapter list selection
        var selectedChapterIndex = ChapterList.ViewModel.SelectedIndex;
        if (ChapterList.listView.Items.Count > 0 
            && selectedChapterIndex >= 0 
            && selectedChapterIndex < ChapterList.listView.Items.Count)
        {
            ChapterList.listView.SelectedIndex = selectedChapterIndex;
        }

        TrySelectPendingChapter();

        // Configure text box appearance
        ChapterText.TextBoxWhiteBackground(Preferences.Get(SettingsValueStrings.TextBoxSolidBackground, false));

        // Load UI state
        LoadTextBoxZoom();
        OnModeSurfaceLayoutChanged(_modeService.Current);

        // Enable/disable tools based on file type
        if (Persistence.CurrentProject?.file is not null)
            UpdateToolsForDocument(Persistence.CurrentProject.file.FileType.Contains(".srl"));
    }

    // ── Event Handlers ──

    private void OnSettingChanged(SettingChangedEvent e)
    {
        if (e.SettingKey == SettingsValueStrings.ZoomValue && ChapterText is not null)
            SetZoomValue(Convert.ToInt32(e.Value));
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.TextFormattingContextActive))
        {
            RefreshFormattingCommandAvailability();
            CommandBar?.RefreshSpeechCommandAvailability();
        }
        else if (e.PropertyName == nameof(MainPageViewModel.ZoomLevel))
        {
            // Update scroll viewer and text box width when zoom changes
            UpdateTextBoxZoom(_viewModel.ZoomLevel);
        }
    }

    private void OnChapterToolsStateChanged(ChapterToolsStateEvent e)
    {
        _viewModel.IsChapterSelected = e.Enabled;

        // Configure text box interaction
        ChapterText.textBox.IsTabStop = e.Enabled;
        ChapterText.textBoxRectangle.IsHitTestVisible = !e.Enabled;
        ChapterText.textBox.IsHitTestVisible = e.Enabled;
        ChapterText.textBoxRectangle.Visibility = e.Enabled ? Visibility.Collapsed : Visibility.Visible;

        RefreshFormattingCommandAvailability();
        CommandBar.searchReplaceButton.IsEnabled = e.Enabled;

        if (!e.Enabled)
            _windowContext.AppView.Focus(FocusState.Keyboard);
    }

    private void OnToggleChapterList(ToggleChapterListEvent e)
    {
        ApplyChapterListLayout(e.Open, e.Manually);
    }

    private void OnModeSurfaceLayoutChanged(IEditorMode mode)
    {
        ApplyChapterListLayoutVisibility(mode);
        ApplyEditorMargin(mode);
    }

    private void OnPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        OnModeSurfaceLayoutChanged(_modeService.Current);
    }

    // ── UI Layout Management ──

    private void ApplyChapterListLayoutVisibility(IEditorMode mode)
    {
        bool showChapterList = mode?.Chrome.ShowChapterList ?? true;
        closeOpenChapterListComponent.Visibility = showChapterList ? Visibility.Visible : Visibility.Collapsed;

        if (!showChapterList)
        {
            SetChapterListLayout(false);
            return;
        }

        ApplyChapterListLayout(ActualWidth >= 800, false);
    }

    private void ApplyChapterListLayout(bool open, bool manually)
    {
        if (!open)
        {
            SetChapterListLayout(false);
            if (!ChapterList.closedManually)
                ChapterList.closedManually = manually;
        }
        else if (!ChapterList.closedManually || manually)
        {
            SetChapterListLayout(true);
            ChapterList.closedManually = false;
        }
    }

    private void SetChapterListLayout(bool open)
    {
        if (!open)
        {
            chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 2);
            mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            mainGrid.ColumnDefinitions[1].MinWidth = 0;
            closeOpenChapterListComponentIcon.Symbol = Symbol.ClosePane;
        }
        else
        {
            chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 1);
            mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            mainGrid.ColumnDefinitions[1].MinWidth = 220;
            closeOpenChapterListComponentIcon.Symbol = Symbol.OpenPane;
        }

        UpdateTextBoxZoom(textBoxZoomSlider.Value);
    }

    private void ApplyEditorMargin(IEditorMode mode)
    {
        double edgeInset = ActualWidth >= 800 ? 20 : 8;
        double topInset = edgeInset;

        if (mode?.Chrome.OverlayContent is not null)
            topInset += mode.Id == "focus" ? 68 : 12;

        chapterTextBoxMainPage.Margin = new Thickness(edgeInset, topInset, edgeInset, edgeInset);
    }

    // ── UI State Management ──

    public void SetTextFormattingContextActive(bool active)
    {
        _viewModel.TextFormattingContextActive = active;
    }

    public void RefreshFormattingCommandAvailability()
    {
        if (CommandBar is null)
            return;

        var enableFormatting = _viewModel.IsChapterSelected
            && _viewModel.TextFormattingContextActive
            && !_viewModel.IsChapterTextReadOnly;

        CommandBar.SetFormattingCommandsEnabled(enableFormatting);

        if (!enableFormatting)
            CommandBar.ClearFormattingCommandState();
    }

    public void UpdateToolsForDocument(bool enableTools)
    {
        ChapterList.canAdd = enableTools;
        ChapterList.listView.IsEnabled = enableTools;
        CommandBar.exportButton.IsEnabled = enableTools;
        CommandBar.charactersButton.IsEnabled = enableTools;
    }

    // ── Chapter Selection ──

    private void TrySelectPendingChapter()
    {
        if (string.IsNullOrWhiteSpace(_pendingChapterToken) || ChapterList?.ViewModel is null)
            return;

        var projectState = App.TryGetService<ProjectState>();
        var chapter = projectState?.FindChapter(_pendingChapterToken);
        if (chapter is null)
            return;

        var chapterIndex = projectState.FindChapterID(_pendingChapterToken);
        ChapterList.ViewModel.SelectedIndex = chapterIndex;

        if (ChapterList.listView is not null && chapterIndex >= 0 && chapterIndex < ChapterList.listView.Items.Count)
        {
            ChapterList.listView.SelectedIndex = chapterIndex;
        }

        _pendingChapterToken = null;
    }

    // ── Notes Pane ──

    public void ToggleNotesPane(bool show)
    {
        _viewModel.NotesPaneVisible = show;

        if (show)
        {
            notesRow.Height = new GridLength(220);
            chapterNotesPane.Visibility = Visibility.Visible;
            chapterNotesPane.LoadNotes();
        }
        else
        {
            notesRow.Height = new GridLength(0);
            chapterNotesPane.Visibility = Visibility.Collapsed;
        }

        if (CommandBar?.notesToggleButton is not null && CommandBar.notesToggleButton.IsChecked != show)
            CommandBar.notesToggleButton.IsChecked = show;
    }

    public void RefreshNotesPane()
    {
        if (chapterNotesPane.Visibility == Visibility.Visible)
            chapterNotesPane.LoadNotes();
    }

    // ── Zoom Management ──

    public void SetZoomValue(int value)
    {
        if (value >= 13 && value <= 100)
            _viewModel.ZoomLevel = value;
    }

    public void LoadTextBoxZoom()
    {
        _viewModel.ZoomLevel = Preferences.Get<double>(SettingsValueStrings.ZoomValue, 25);
    }

    /// <summary>
    /// Updates the scroll viewer zoom and text box width based on zoom level.
    /// Called when ZoomLevel property changes in the ViewModel.
    /// </summary>
    private void UpdateTextBoxZoom(double sliderValue)
    {
        double scale = sliderValue / 25;
        _ = ChapterText.textBoxScrollViewer.ChangeView(null, null, (float)scale);

        double viewportWidth = ChapterText.textBoxScrollViewer.ActualWidth;
        if (viewportWidth > 0 && scale > 0)
        {
            double desiredWidth = viewportWidth * (1 / scale);
            ChapterText.textBox.Width = Math.Max(desiredWidth, viewportWidth);
        }
    }

    private void OnTextBoxZoomText_Click(object sender, RoutedEventArgs e)
    {
        textBoxZoomTextFlyout.ShowAt(textBoxZoomText);
        textBoxZoomTextFlyoutTextBox.Value = _viewModel.ZoomLevel * 4;
    }

    private void OnTextBoxZoomTextFlyoutTextBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
    {
        if (!double.IsNaN(sender.Value))
            _viewModel.ZoomLevel = sender.Value / 4;
    }

    // ── Session Timer ──

    public void StartSessionTimer()
    {
        int wordCount = RtfHelper.GetTotalWordCount(App.GetService<ProjectState>().Chapters);
        if (!_viewModel.StartSession(wordCount)) 
            return;

        _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _sessionTimer.Tick += (s, _) => _viewModel.OnSessionTimerTick();
        _sessionTimer.Start();
    }

    public void UpdateDownBar() => _viewModel.UpdateDownBar();

    public void ShowWelcomePanel(bool show) => _viewModel.ShowWelcomePanel(show);
}
