namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the ChapterTagsDialogue.
/// Manages tag editing, suggestions, and saved presets.
/// </summary>
public partial class ChapterTagsViewModel : ObservableObject
{
    private readonly ProjectState _projectState;
    private readonly Chapter _chapter;

    [ObservableProperty] private string _chapterName = string.Empty;
    [ObservableProperty] private List<string> _suggestions = new();
    [ObservableProperty] private List<string> _savedPresets = new();
    [ObservableProperty] private bool _hasNoPresets = true;

    public List<string> CurrentTags { get; set; } = new();

    public ChapterTagsViewModel(
        ProjectState projectState,
        Chapter chapter)
    {
        _projectState = projectState;
        _chapter = chapter;
        ChapterName = chapter?.Name ?? string.Empty;
    }

    public void Initialize()
    {
        CurrentTags = _chapter?.Tags?.ToList() ?? new List<string>();
        RefreshSuggestions();
        RefreshSavedPresets();
    }

    public void RefreshSuggestions()
    {
        Suggestions = ChapterTagsService
            .GetAllSuggestions(_projectState.Chapters)
            .Where(s => !CurrentTags.Contains(s, StringComparer.CurrentCultureIgnoreCase))
            .Take(12)
            .ToList();
    }

    public void RefreshSavedPresets()
    {
        SavedPresets = ChapterTagsService.GetPresets().ToList();
        HasNoPresets = SavedPresets.Count == 0;
    }

    [RelayCommand]
    private void AddSuggestion(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !CurrentTags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
        {
            CurrentTags.Add(tag);
            RefreshSuggestions();
        }
    }

    [RelayCommand]
    private void RemovePreset(string preset)
    {
        if (!string.IsNullOrWhiteSpace(preset))
        {
            ChapterTagsService.RemovePreset(preset);
            RefreshSuggestions();
            RefreshSavedPresets();
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (_chapter is null) return;

        var newTags = CurrentTags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _chapter.Tags = newTags;

        foreach (var tag in Enumerable.Reverse(newTags))
            ChapterTagsService.AddPreset(tag);

        TimeTravelSystem.SomethingChanged();
    }

    public IEnumerable<string> GetAutoSuggestions(string query)
    {
        var allSuggestions = ChapterTagsService.GetAllSuggestions(_projectState.Chapters);
        return string.IsNullOrWhiteSpace(query)
            ? allSuggestions
            : allSuggestions.Where(s => s.StartsWith(query, StringComparison.CurrentCultureIgnoreCase));
    }
}
