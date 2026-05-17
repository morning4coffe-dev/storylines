using Storylines.Services.Modes;

namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the ChapterTextBox control.
/// Manages formatting state, search/replace logic, dialogue popup,
/// and typewriter mode — all previously in code-behind.
/// </summary>
public partial class ChapterTextBoxViewModel : ObservableObject
{
    private readonly EventAggregator _events;
    private readonly ProjectState _projectState;
    private readonly ITextEditorService _textEditor;
    private readonly IWritingSessionService _writingSession;
    private readonly IPreferencesService _preferences;
    private readonly IDialogService _dialogs;
    private readonly ResourceLoader _resources;

    // ── Formatting state ──

    [ObservableProperty] private bool _isBold;
    [ObservableProperty] private bool _isItalic;
    [ObservableProperty] private bool _isUnderlined;
    [ObservableProperty] private bool _isStrikethrough;

    // ── Search & Replace state ──

    [ObservableProperty] private bool _isSearchActive;
    [ObservableProperty] private bool _isSearchReplaceOpen;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _searchReplaceFindText = string.Empty;
    [ObservableProperty] private string _searchReplaceText = string.Empty;
    [ObservableProperty] private string _searchMatchCountText = string.Empty;
    [ObservableProperty] private bool _isMatchCase;
    [ObservableProperty] private bool _isWholeWord;
    [ObservableProperty] private bool _isAllChaptersScope;
    [ObservableProperty] private string _replaceAllButtonContent;

    private int _currentMatchIndex = -1;
    private int _totalMatches = 0;

    // ── Dialogue popup state ──

    [ObservableProperty] private bool _dialoguesEnabled;
    [ObservableProperty] private bool _isDialoguePopupOpen;

    public ObservableCollection<Character> DialoguePopupCharacters { get; } = new();
    private readonly ObservableCollection<string> _recentDialogueCharacterTokens = new();

    /// <summary>
    /// True when the popup was triggered by the Enter key — newline was already inserted,
    /// so InsertDialogue must not prepend another one.
    /// </summary>
    public bool DialoguePopupEnteredViaKey { get; set; }

    // ── Typewriter mode ──

    [ObservableProperty] private bool _isTypewriterModeActive;

    // ── Chapter tracking ──

    [ObservableProperty] private string _loadedChapterToken;

    // ── Read-only & chrome state ──

    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private bool _isFormattingBarVisible = true;

    public ChapterTextBoxViewModel(
        EventAggregator events,
        ProjectState projectState,
        ITextEditorService textEditor,
        IWritingSessionService writingSession,
        IPreferencesService preferences,
        IDialogService dialogs)
    {
        _events = events;
        _projectState = projectState;
        _textEditor = textEditor;
        _writingSession = writingSession;
        _preferences = preferences;
        _dialogs = dialogs;
        _resources = ResourceLoader.GetForViewIndependentUse();

        DialoguesEnabled = SettingsValues.dialogueModeEnabled;
        UpdateReplaceAllButtonContent();

        _events.Subscribe<ChapterSelectedEvent>(OnChapterSelected);
    }

    // ── Event handlers ──

    private void OnChapterSelected(ChapterSelectedEvent e)
    {
        LoadedChapterToken = e.HasSelection
            && e.SelectedIndex >= 0
            && e.SelectedIndex < _projectState.Chapters.Count
            ? _projectState.Chapters[e.SelectedIndex].Token
            : null;
    }

    // ── Formatting commands ──

    public void UpdateFormattingState(bool bold, bool italic, bool underlined, bool strikethrough)
    {
        IsBold = bold;
        IsItalic = italic;
        IsUnderlined = underlined;
        IsStrikethrough = strikethrough;
        PublishFormattingState();
    }

    private void PublishFormattingState()
    {
        _events.Publish(new TextFormattingStateChangedEvent
        {
            IsBold = IsBold,
            IsItalic = IsItalic,
            IsUnderlined = IsUnderlined,
            IsStrikethrough = IsStrikethrough,
        });
    }

    [RelayCommand]
    private void ToggleBold() => RequestFormatToggle("Bold");

    [RelayCommand]
    private void ToggleItalic() => RequestFormatToggle("Italic");

    [RelayCommand]
    private void ToggleUnderline() => RequestFormatToggle("Underline");

    [RelayCommand]
    private void ToggleStrikethrough() => RequestFormatToggle("Strikethrough");

    [RelayCommand]
    private void ApplyHighlight() => RequestFormatToggle("Highlighter");

    /// <summary>
    /// Raised when the view should apply a formatting toggle to the RichEditBox.
    /// The view subscribes to this and performs the actual text manipulation.
    /// </summary>
    public event Action<string> FormatToggleRequested;

    private void RequestFormatToggle(string formatType)
    {
        FormatToggleRequested?.Invoke(formatType);
    }

    // ── Search & Replace ──

    partial void OnIsAllChaptersScopeChanged(bool value)
    {
        UpdateReplaceAllButtonContent();
    }

    private void UpdateReplaceAllButtonContent()
    {
        var replaceAllText = _resources.GetString("replaceAllButton.Content");
        if (IsAllChaptersScope)
        {
            var allChaptersText = _resources.GetString("allChaptersScopeToggle.Content");
            ReplaceAllButtonContent = $"{replaceAllText} · {allChaptersText}";
        }
        else
        {
            ReplaceAllButtonContent = replaceAllText;
        }
    }

    [RelayCommand]
    private void OpenSearch()
    {
        IsSearchActive = true;
        SearchRequested?.Invoke();
    }

    [RelayCommand]
    private void OpenSearchAndReplace()
    {
        IsSearchReplaceOpen = true;
        IsSearchActive = true;
        SearchReplaceOpenRequested?.Invoke();
    }

    [RelayCommand]
    private void CloseSearchAndReplace()
    {
        IsSearchActive = false;
        IsSearchReplaceOpen = false;
        SearchReplaceCloseRequested?.Invoke();
    }

    [RelayCommand]
    private void FindNext() => NavigateMatchRequested?.Invoke(true);

    [RelayCommand]
    private void FindPrevious() => NavigateMatchRequested?.Invoke(false);

    [RelayCommand]
    private void ReplaceSingle() => ReplaceSingleRequested?.Invoke();

    [RelayCommand]
    private void ReplaceAll() => ReplaceAllRequested?.Invoke();

    [RelayCommand]
    private void ToggleMatchCase()
    {
        IsMatchCase = !IsMatchCase;
        SearchHighlightRequested?.Invoke();
    }

    [RelayCommand]
    private void ToggleWholeWord()
    {
        IsWholeWord = !IsWholeWord;
        SearchHighlightRequested?.Invoke();
    }

    /// <summary>Events for the view to perform RichEditBox-specific operations.</summary>
    public event Action SearchRequested;
    public event Action SearchReplaceOpenRequested;
    public event Action SearchReplaceCloseRequested;
    public event Action<bool> NavigateMatchRequested;
    public event Action ReplaceSingleRequested;
    public event Action ReplaceAllRequested;
    public event Action SearchHighlightRequested;

    public void UpdateMatchCount(int total, int current = -1)
    {
        _totalMatches = total;
        _currentMatchIndex = current;

        if (total > 0)
            SearchMatchCountText = $"{total} match{(total == 1 ? "" : "es")}";
        else if (!string.IsNullOrEmpty(SearchReplaceFindText))
            SearchMatchCountText = _resources.GetString("noMatchesFound");
        else
            SearchMatchCountText = string.Empty;
    }

    public void SetReplacementResultText(string text)
    {
        SearchMatchCountText = text;
    }

    // ── Dialogue popup ──

    [RelayCommand]
    private void ToggleDialogueMode()
    {
        DialoguesEnabled = !DialoguesEnabled;
        _preferences.Set(SettingsValueStrings.DialogueModeEnabled, DialoguesEnabled);

        if (DialoguesEnabled && !SettingsValues.dialogueTeachingTipShown)
        {
            DialogueTeachingTipRequested?.Invoke();
            _preferences.Set(SettingsValueStrings.DialogueTeachingTipShown, true);
        }
    }

    [RelayCommand]
    private void ShowDialoguePopup()
    {
        if (_projectState.Characters.Count == 0)
        {
            _ = _dialogs.ShowNoCharactersDialogueAsync();
            return;
        }

        RefreshDialoguePopupCharacters();
        IsDialoguePopupOpen = true;
    }

    public void RefreshDialoguePopupCharacters()
    {
        DialoguePopupCharacters.Clear();

        // Show recent characters first
        foreach (var token in _recentDialogueCharacterTokens)
        {
            var character = _projectState.Characters.FirstOrDefault(c => c.Token == token);
            if (character is not null)
                DialoguePopupCharacters.Add(character);
        }

        // Then add remaining characters alphabetically
        var recentTokens = _recentDialogueCharacterTokens.ToHashSet();
        foreach (var character in _projectState.Characters
            .Where(c => !recentTokens.Contains(c.Token))
            .OrderBy(c => c.Name))
        {
            DialoguePopupCharacters.Add(character);
        }
    }

    public void RememberRecentCharacter(Character character)
    {
        if (character is null) return;

        if (_recentDialogueCharacterTokens.Contains(character.Token))
            _recentDialogueCharacterTokens.Remove(character.Token);

        _recentDialogueCharacterTokens.Insert(0, character.Token);

        while (_recentDialogueCharacterTokens.Count > 4)
            _recentDialogueCharacterTokens.RemoveAt(_recentDialogueCharacterTokens.Count - 1);
    }

    /// <summary>Raised when the dialogue teaching tip should be shown.</summary>
    public event Action DialogueTeachingTipRequested;

    // ── Text change handling ──

    /// <summary>
    /// Called by the view when the RichEditBox text changes.
    /// Manages model sync, undo recording, and session tracking.
    /// </summary>
    public void OnTextChanged(string newRtf, string plainText, Chapter chapter)
    {
        if (chapter is null) return;

        var oldText = chapter.Text;
        if (oldText != newRtf && !IsSearchActive)
        {
            chapter.Text = newRtf;
            TimeTravelChapter.RecordTextChange(chapter.Token, oldText, newRtf);

            App.TryGetService<EditorModeService>()?.Current.OnTextChanged();

            int words = plainText.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            _writingSession.RecordWords(words);

            TextChangedForView?.Invoke();
        }
    }

    /// <summary>Raised so the view can trigger session timer, word goal, and location capture.</summary>
    public event Action TextChangedForView;

    // ── Chapter access ──

    public Chapter GetLoadedChapter()
    {
        if (string.IsNullOrWhiteSpace(LoadedChapterToken))
            return null;

        return _projectState.FindChapter(LoadedChapterToken);
    }
}
