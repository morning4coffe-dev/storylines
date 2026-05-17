using Microsoft.UI.Xaml.Media.Imaging;

namespace Storylines.ViewModels;

public partial class CharactersPageViewModel : ObservableObject
{
    private readonly ProjectState _projectState;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly ResourceLoader _resources;

    public ObservableCollection<Character> Characters => _projectState.Characters;
    public ObservableCollection<Character> FilteredCharacters { get; } = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private Visibility _noCharactersVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _noSearchResultsVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private List<RelationshipDisplayItem> _relationshipItems = new();

    [ObservableProperty]
    private Visibility _noRelationshipsVisibility = Visibility.Visible;

    [ObservableProperty]
    private bool _isAddRelationshipEnabled;

    [ObservableProperty]
    private Character _selectedCharacter;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isAddEnabled = true;

    [ObservableProperty]
    private bool _isRemoveEnabled = true;

    [ObservableProperty]
    private string _editButtonLabel;

    [ObservableProperty]
    private string _editButtonGlyph = "\uE104";

    [ObservableProperty]
    private Visibility _cancelButtonVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isListEnabled = true;

    [ObservableProperty]
    private bool _isFieldsEnabled;

    [ObservableProperty]
    private string _nameText = string.Empty;

    [ObservableProperty]
    private string _descriptionText = string.Empty;

    [ObservableProperty]
    private string _roleText = string.Empty;

    [ObservableProperty]
    private string _ageText = string.Empty;

    [ObservableProperty]
    private string _traitsText = string.Empty;

    [ObservableProperty]
    private string _appearanceText = string.Empty;

    [ObservableProperty]
    private BitmapImage _profilePicture;

    private CharacterPicture _pictureData;

    [ObservableProperty]
    private bool _isCharacterSelected;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private string _dialogueNodeCountText;

    public bool UnappliedChanges { get; set; }

    private Character _characterBeforeChange;

    public CharactersPageViewModel(
        ProjectState projectState,
        EventAggregator events,
        INavigationService navigation,
        IDialogService dialogs)
    {
        _projectState = projectState;
        _navigation = navigation;
        _dialogs = dialogs;
        _resources = ResourceLoader.GetForViewIndependentUse();
        EditButtonLabel = _resources.GetString("editText");

        events.Subscribe<UndoRedoStateChangedEvent>(OnUndoRedoStateChanged);

        events.Subscribe<CharacterSelectedEvent>(e =>
        {
            if (e.HasSelection && e.SelectedIndex >= 0 && e.SelectedIndex < _projectState.Characters.Count)
            {
                var selectedToken = _projectState.Characters[e.SelectedIndex].Token;
                CharacterSelectedFromUndo?.Invoke(selectedToken);
            }
        });
    }

    /// <summary>Raised when undo/redo selects a character, passes token.</summary>
    public event Action<string> CharacterSelectedFromUndo;

    private void OnUndoRedoStateChanged(UndoRedoStateChangedEvent e)
    {
        if (e.Context == "characters")
        {
            CanUndo = e.CanUndo;
            CanRedo = e.CanRedo;
        }
    }

    partial void OnSelectedCharacterChanged(Character value)
    {
        IsCharacterSelected = value is not null;
        if (value is not null)
        {
            NameText = value.Name ?? string.Empty;
            DescriptionText = value.Description ?? string.Empty;
            RoleText = value.Role ?? string.Empty;
            AgeText = value.Age ?? string.Empty;
            TraitsText = value.TraitsText ?? string.Empty;
            AppearanceText = value.Appearance ?? string.Empty;
            ProfilePicture = value.Picture?.Image;
            _pictureData = value.Picture;
            UpdateDialogueNodeCount(value);
        }
    }

    private void UpdateDialogueNodeCount(Character character)
    {
        DialogueNodeCountText = null;
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        if (IsEditMode)
        {
            // Leaving edit mode - apply changes if something changed
            if (SelectedCharacter is not null && DidSomethingChange())
            {
                ApplyChanges();
                SortCharacters();
            }
            ExitEditMode();
        }
        else
        {
            EnterEditMode();
        }
    }

    public void EnterEditMode()
    {
        if (SelectedCharacter is null) return;

        IsEditMode = true;
        IsFieldsEnabled = true;
        IsListEnabled = false;

        _characterBeforeChange = _projectState.CopyCharacter(SelectedCharacter.Token);

        CancelButtonVisibility = Visibility.Collapsed;
        EditButtonLabel = _resources.GetString("cancelText");
        EditButtonGlyph = "\uE10A";
        UnappliedChanges = false;
    }

    public void ExitEditMode()
    {
        IsEditMode = false;
        IsFieldsEnabled = false;
        IsListEnabled = true;

        CancelButtonVisibility = Visibility.Collapsed;
        EditButtonLabel = _resources.GetString("editText");
        EditButtonGlyph = "\uE104";
        UnappliedChanges = false;
    }

    public void MarkUnappliedChanges()
    {
        UnappliedChanges = true;
        CancelButtonVisibility = Visibility.Visible;
        EditButtonLabel = _resources.GetString("applyChanges");
        EditButtonGlyph = "\uE081";
    }

    public void MarkCleanEditMode()
    {
        UnappliedChanges = false;
        CancelButtonVisibility = Visibility.Collapsed;
        EditButtonLabel = _resources.GetString("cancelText");
        EditButtonGlyph = "\uE10A";
    }

    public void ApplyChanges()
    {
        if (SelectedCharacter is not null && _characterBeforeChange is not null)
        {
            TimeTravelCharacter.RecordChanged(_characterBeforeChange);
            UnappliedChanges = false;

            SelectedCharacter.Name = NameText;
            SelectedCharacter.Description = DescriptionText;
            SelectedCharacter.Role = RoleText;
            SelectedCharacter.Age = AgeText;
            SelectedCharacter.TraitsText = TraitsText;
            SelectedCharacter.Appearance = AppearanceText;
            if (_pictureData is not null)
                SelectedCharacter.Picture = _pictureData;
        }
    }

    [RelayCommand]
    public void CancelEdit()
    {
        if (_characterBeforeChange is not null)
        {
            NameText = _characterBeforeChange.Name;
            DescriptionText = _characterBeforeChange.Description;
            RoleText = _characterBeforeChange.Role ?? string.Empty;
            AgeText = _characterBeforeChange.Age ?? string.Empty;
            TraitsText = _characterBeforeChange.TraitsText ?? string.Empty;
            AppearanceText = _characterBeforeChange.Appearance ?? string.Empty;
            ProfilePicture = _characterBeforeChange.Picture?.Image;
            _pictureData = _characterBeforeChange.Picture;
        }

        ExitEditMode();
    }

    [RelayCommand]
    private void AddCharacter()
    {
        var ch = _projectState.CreateNewCharacter(
            _resources.GetString("newCharacterName"),
            string.Empty);
    }

    [RelayCommand]
    private void RemoveCharacter()
    {
        if (SelectedCharacter is not null)
        {
            _projectState.RemoveCharacter(SelectedCharacter.Token);
        }
    }

    [RelayCommand]
    private void Undo()
    {
        TimeTravelCharacter.Undo();
    }

    [RelayCommand]
    private void Redo()
    {
        TimeTravelCharacter.Redo();
    }

    public bool DidSomethingChange()
    {
        if (SelectedCharacter is null) return false;
        return SelectedCharacter.Name != NameText
            || SelectedCharacter.Description != DescriptionText
            || SelectedCharacter.Role != (string.IsNullOrEmpty(RoleText) ? null : RoleText)
            || SelectedCharacter.Age != (string.IsNullOrEmpty(AgeText) ? null : AgeText)
            || SelectedCharacter.TraitsText != TraitsText
            || SelectedCharacter.Appearance != AppearanceText
            || SelectedCharacter.Picture?.Image != ProfilePicture;
    }

    private void SortCharacters()
    {
        _projectState.SortCharacters();
        OnPropertyChanged(nameof(Characters));
    }

    public void SetPicture(CharacterPicture picture, BitmapImage image)
    {
        _pictureData = picture;
        ProfilePicture = image;
    }

    // ─── Search / filter ───────────────────────────────────────────

    public void RefreshFilteredList(string selectedToken = null)
    {
        var query = (SearchQuery ?? string.Empty).Trim();
        var matching = Characters
            .Where(c => MatchesSearch(c, query))
            .ToList();

        FilteredCharacters.Clear();
        foreach (var c in matching)
            FilteredCharacters.Add(c);

        UpdateEmptyState();

        // Raise event so the view can sync ListView selection
        FilteredListRefreshed?.Invoke(selectedToken);
    }

    private static bool MatchesSearch(Character character, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var searchTarget = string.Join(" ", new[]
        {
            character?.Name,
            character?.Role,
            character?.Age,
            character?.TraitsText,
            character?.Appearance,
            character?.Description,
        });

        return searchTarget?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    public void UpdateEmptyState()
    {
        if (Characters.Count == 0)
        {
            NoCharactersVisibility = Visibility.Visible;
            NoSearchResultsVisibility = Visibility.Collapsed;
        }
        else if (FilteredCharacters.Count == 0)
        {
            NoCharactersVisibility = Visibility.Collapsed;
            NoSearchResultsVisibility = Visibility.Visible;
        }
        else
        {
            NoCharactersVisibility = Visibility.Collapsed;
            NoSearchResultsVisibility = Visibility.Collapsed;
        }
    }

    /// <summary>Raised after FilteredCharacters is rebuilt. Passes selectedToken for view to restore selection.</summary>
    public event Action<string> FilteredListRefreshed;

    // ─── Relationships ─────────────────────────────────────────────

    public void LoadRelationships(Character character)
    {
        var items = new List<RelationshipDisplayItem>();

        if (character?.Relationships is not null)
        {
            foreach (var rel in character.Relationships)
            {
                var target = _projectState.FindCharacter(rel.TargetCharacterToken);
                items.Add(new RelationshipDisplayItem
                {
                    DisplayText = target?.Name ?? "(unknown)",
                    Type = rel.Type ?? "",
                    TargetToken = rel.TargetCharacterToken
                });
            }
        }

        RelationshipItems = items;
        NoRelationshipsVisibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        IsAddRelationshipEnabled = character is not null;
    }

    public async System.Threading.Tasks.Task AddRelationshipAsync(Character character)
    {
        if (character is null) return;

        var otherCharacters = Characters.Where(c => c.Token != character.Token).ToList();
        if (otherCharacters.Count == 0) return;

        var targetCombo = new Microsoft.UI.Xaml.Controls.ComboBox
        {
            PlaceholderText = _resources.GetString("relationshipSelectCharacter"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = otherCharacters.Select(c => c.Name).ToList()
        };
        var typeBox = new Microsoft.UI.Xaml.Controls.TextBox
        {
            PlaceholderText = _resources.GetString("relationshipTypePlaceholder")
        };

        var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 8 };
        panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = _resources.GetString("relationshipCharacterLabel") });
        panel.Children.Add(targetCombo);
        panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = _resources.GetString("relationshipTypeLabel") });
        panel.Children.Add(typeBox);

        var result = await _dialogs.ShowMessageAsync(new DialogDefinition
        {
            Title = _resources.GetString("addRelationshipTitle"),
            Content = panel,
            PrimaryButtonText = _resources.GetString("addButtonText"),
            CloseButtonText = _resources.GetString("cancelButtonText"),
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
        });

        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary && targetCombo.SelectedIndex >= 0)
        {
            var target = otherCharacters[targetCombo.SelectedIndex];
            character.Relationships.Add(new CharacterRelationship
            {
                TargetCharacterToken = target.Token,
                Type = typeBox.Text?.Trim() ?? ""
            });
            TimeTravelSystem.SomethingChanged();
            LoadRelationships(character);
        }
    }

    public void RemoveRelationship(Character character, string targetToken)
    {
        if (character is null || string.IsNullOrEmpty(targetToken)) return;

        var rel = character.Relationships.FirstOrDefault(r => r.TargetCharacterToken == targetToken);
        if (rel is not null)
        {
            character.Relationships.Remove(rel);
            TimeTravelSystem.SomethingChanged();
            LoadRelationships(character);
        }
    }

    // ─── Add character ─────────────────────────────────────────────

    public Character AddNewCharacter()
    {
        int value = Random.Shared.Next(0, 2);
        var name = value == 1 ? _resources.GetString("johnDoe") : _resources.GetString("janeDoe");
        return _projectState.CreateNewCharacter(name, "");
    }

    // ─── Navigation ────────────────────────────────────────────────

    public void NavigateToChapter(string chapterToken)
    {
        if (!string.IsNullOrWhiteSpace(chapterToken))
            _navigation?.NavigateTo(NavigationTarget.MainPage, chapterToken);
    }

    // ─── Trait suggestions ─────────────────────────────────────────

    public List<string> GetTraitSuggestions(string query)
    {
        var allTraits = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var ch in _projectState.Characters)
            foreach (var trait in ch.Traits ?? new List<string>())
                allTraits.Add(trait);

        if (string.IsNullOrWhiteSpace(query))
            return new List<string>(allTraits);

        return new List<string>(allTraits.Where(t =>
            t.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)));
    }
}

public class RelationshipDisplayItem
{
    public string DisplayText { get; set; }
    public string Type { get; set; }
    public string TargetToken { get; set; }
}
