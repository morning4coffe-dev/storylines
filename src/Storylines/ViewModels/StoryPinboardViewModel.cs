namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the Story Pinboard page.
/// Manages card layout, filtering, drag/drop, chapter connections, and subtitle.
/// </summary>
public partial class StoryPinboardViewModel : ObservableObject
{
    private readonly ProjectState _projectState;
    private readonly ITextEditorService _textEditor;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly ResourceLoader _resources;

    public ObservableCollection<Chapter> AllChapters => _projectState.Chapters;

    [ObservableProperty]
    private List<Chapter> _filteredChapters = new();

    [ObservableProperty]
    private string _activeTagFilter;

    [ObservableProperty]
    private List<string> _availableTags = new();

    [ObservableProperty]
    private int _selectedTagIndex;

    [ObservableProperty]
    private string _subtitleText = string.Empty;

    [ObservableProperty]
    private Visibility _emptyStateVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _tagFilterVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isConnectMode;

    // Connect mode state
    private Chapter _connectSource;

    public StoryPinboardViewModel(
        ProjectState projectState,
        ITextEditorService textEditor,
        INavigationService navigation,
        IDialogService dialogs)
    {
        _projectState = projectState;
        _textEditor = textEditor;
        _navigation = navigation;
        _dialogs = dialogs;
        _resources = ResourceLoader.GetForViewIndependentUse();
    }

    /// <summary>
    /// Initializes the pinboard data. Called when the page loads.
    /// </summary>
    public void Initialize()
    {
        ActiveTagFilter = null;
        SelectedTagIndex = 0;
        PopulateTagFilter();
        ApplyFilter();
    }

    /// <summary>
    /// Populates the tag filter combo box options.
    /// </summary>
    public void PopulateTagFilter()
    {
        var allTags = AllChapters
            .Where(c => c.Tags is not null)
            .SelectMany(c => c.Tags)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var tags = new List<string> { _resources.GetString("allChaptersFilterLabel") ?? "All chapters" };
        tags.AddRange(allTags);
        AvailableTags = tags;

        TagFilterVisibility = allTags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    partial void OnSelectedTagIndexChanged(int value)
    {
        ActiveTagFilter = value <= 0 ? null : (value < AvailableTags.Count ? AvailableTags[value] : null);
        ApplyFilter();
    }

    /// <summary>
    /// Applies current filter and updates the filtered chapter list.
    /// </summary>
    public void ApplyFilter()
    {
        IEnumerable<Chapter> source = AllChapters;

        if (!string.IsNullOrEmpty(ActiveTagFilter))
        {
            source = AllChapters.Where(c =>
                c.Tags is not null &&
                c.Tags.Any(t => string.Equals(t, ActiveTagFilter, StringComparison.CurrentCultureIgnoreCase)));
        }

        FilteredChapters = source.ToList();
        EmptyStateVisibility = FilteredChapters.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateSubtitle();

        CanvasRebuildRequested?.Invoke();
    }

    private void UpdateSubtitle()
    {
        int total = AllChapters.Count;
        int shown = FilteredChapters.Count;

        if (string.IsNullOrEmpty(ActiveTagFilter))
            SubtitleText = $"{total} chapter{(total == 1 ? "" : "s")}";
        else
            SubtitleText = $"{shown} of {total} chapter{(total == 1 ? "" : "s")} tagged \"{ActiveTagFilter}\"";
    }

    /// <summary>
    /// Assigns default grid positions for chapters that have none.
    /// </summary>
    public bool AssignDefaultPositions()
    {
        const double startX = 40;
        const double startY = 40;
        const double spacingX = 240;
        const double spacingY = 220;
        const int columns = 5;

        int unpositioned = 0;
        foreach (var chapter in FilteredChapters)
        {
            if (chapter.PinboardX == 0 && chapter.PinboardY == 0)
            {
                int idx = AllChapters.IndexOf(chapter);
                int col = idx % columns;
                int row = idx / columns;
                chapter.PinboardX = startX + col * spacingX;
                chapter.PinboardY = startY + row * spacingY;
                unpositioned++;
            }
        }

        if (unpositioned > 0)
        {
            TimeTravelSystem.SomethingChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Detects characters mentioned in a chapter's text.
    /// </summary>
    public List<string> DetectCharactersInChapter(Chapter chapter)
    {
        var characters = _projectState.Characters;
        if (characters is null || characters.Count == 0 || string.IsNullOrEmpty(chapter.Text))
            return new List<string>();

        var found = new List<string>();
        string textLower = (chapter.Text ?? "").ToLowerInvariant();
        foreach (var c in characters)
        {
            if (!string.IsNullOrWhiteSpace(c.Name) && textLower.Contains(c.Name.ToLowerInvariant()))
                found.Add(c.Name);
        }
        return found;
    }

    // ─── Connection management ─────────────────────────────────────

    /// <summary>
    /// Handles a card click during connect mode.
    /// First click selects source, second click creates connection.
    /// Returns (action, fromIdx, toIdx) for the view to handle visual updates.
    /// </summary>
    public (ConnectAction action, int fromIdx, int toIdx) HandleConnectClick(Chapter chapter)
    {
        if (_connectSource is null)
        {
            _connectSource = chapter;
            return (ConnectAction.HighlightSource, -1, -1);
        }

        if (_connectSource.Token == chapter.Token)
        {
            var source = _connectSource;
            _connectSource = null;
            return (ConnectAction.ClearHighlight, -1, -1);
        }

        int fromIdx = AllChapters.IndexOf(_connectSource);
        int toIdx = AllChapters.IndexOf(chapter);
        _connectSource = null;

        if (fromIdx >= 0 && toIdx >= 0)
            return (ConnectAction.CreateConnection, fromIdx, toIdx);

        return (ConnectAction.ClearHighlight, -1, -1);
    }

    public Chapter ConnectSource => _connectSource;

    public void ClearPendingConnection()
    {
        _connectSource = null;
    }

    public async System.Threading.Tasks.Task AddConnectionAsync(int fromIndex, int toIndex)
    {
        var connections = _projectState.PinboardConnections;

        bool exists = connections.Any(c =>
            (c.FromIndex == fromIndex && c.ToIndex == toIndex) ||
            (c.FromIndex == toIndex && c.ToIndex == fromIndex));

        if (exists) return;

        var inputBox = new Microsoft.UI.Xaml.Controls.TextBox
        {
            PlaceholderText = _resources.GetString("connectionLabelPlaceholder"),
            AcceptsReturn = false
        };

        var result = await _dialogs.ShowMessageAsync(new DialogDefinition
        {
            Title = _resources.GetString("connectionLabelTitle"),
            Content = inputBox,
            PrimaryButtonText = _resources.GetString("createButtonText"),
            CloseButtonText = _resources.GetString("skipButtonText"),
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
        });

        string label = null;
        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputBox.Text))
            label = inputBox.Text.Trim();

        connections.Add(new PinboardConnectionData { FromIndex = fromIndex, ToIndex = toIndex, Label = label });
        TimeTravelSystem.SomethingChanged();

        ConnectionsChangedForView?.Invoke();
    }

    public void RemoveConnection(int fromIndex, int toIndex)
    {
        var connections = _projectState.PinboardConnections;
        int removed = connections.RemoveAll(c =>
            (c.FromIndex == fromIndex && c.ToIndex == toIndex) ||
            (c.FromIndex == toIndex && c.ToIndex == fromIndex));

        if (removed > 0)
        {
            TimeTravelSystem.SomethingChanged();
            ConnectionsChangedForView?.Invoke();
        }
    }

    public int FindChapterIndex(string token)
    {
        for (int i = 0; i < AllChapters.Count; i++)
            if (AllChapters[i].Token == token) return i;
        return -1;
    }

    // ─── Auto-arrange ──────────────────────────────────────────────

    [RelayCommand]
    public void AutoArrangeChapters()
    {
        const double startX = 40;
        const double startY = 40;
        const double spacingX = 240;
        const double spacingY = 220;
        const int columns = 5;

        for (int i = 0; i < FilteredChapters.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            FilteredChapters[i].PinboardX = startX + col * spacingX;
            FilteredChapters[i].PinboardY = startY + row * spacingY;
        }

        TimeTravelSystem.SomethingChanged();
        CanvasRebuildRequested?.Invoke();
    }

    // ─── Navigation ────────────────────────────────────────────────

    [RelayCommand]
    public void NavigateToChapter(Chapter chapter)
    {
        if (chapter is null) return;

        int index = AllChapters.IndexOf(chapter);
        if (index < 0) return;

        _navigation.GoBack();
        _textEditor.SelectedChapterIndex = index;

        ChapterNavigated?.Invoke(index);
    }

    // ─── Drag completion ───────────────────────────────────────────

    public void OnDragCompleted()
    {
        TimeTravelSystem.SomethingChanged();
    }

    // ─── Events for view ───────────────────────────────────────────

    /// <summary>Raised when the canvas needs to be rebuilt.</summary>
    public event Action CanvasRebuildRequested;

    /// <summary>Raised when connections changed and need redrawing.</summary>
    public event Action ConnectionsChangedForView;

    /// <summary>Raised when navigating to a chapter, passes index.</summary>
    public event Action<int> ChapterNavigated;
}

public enum ConnectAction
{
    HighlightSource,
    ClearHighlight,
    CreateConnection
}
