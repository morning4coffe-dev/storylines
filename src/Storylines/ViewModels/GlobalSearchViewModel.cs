namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the GlobalSearchDialogue.
/// Manages search query, results, and navigation actions.
/// </summary>
public partial class GlobalSearchViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly ProjectState _projectState;
    private readonly ITextEditorService _textEditor;
    private readonly ResourceLoader _resources;
    private readonly List<GlobalSearchResultItem> _quickActions;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private List<GlobalSearchResultItem> _results = new();
    [ObservableProperty] private Visibility _noResultsVisibility = Visibility.Collapsed;

    public GlobalSearchViewModel(
        INavigationService navigation,
        ProjectState projectState,
        ITextEditorService textEditor)
    {
        _navigation = navigation;
        _projectState = projectState;
        _textEditor = textEditor;
        _resources = ResourceLoader.GetForViewIndependentUse();
        _quickActions = BuildQuickActions().ToList();
    }

    partial void OnSearchQueryChanged(string value)
    {
        RefreshResults();
    }

    public void RefreshResults()
    {
        var query = SearchQuery?.Trim();
        var results = new List<GlobalSearchResultItem>();

        results.AddRange(GetQuickActions(query));

        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
            results.AddRange(GetChapterResults(query));

        Results = results;
        NoResultsVisibility = results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    [RelayCommand]
    private void ExecuteResult(GlobalSearchResultItem result)
    {
        result?.Execute?.Invoke();
    }

    private IEnumerable<GlobalSearchResultItem> GetQuickActions(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _quickActions;

        return _quickActions.Where(r => r.Matches(query));
    }

    private IEnumerable<GlobalSearchResultItem> GetChapterResults(string query)
    {
        for (int i = 0; i < _projectState.Chapters.Count; i++)
        {
            var chapter = _projectState.Chapters[i];
            string chapterTitle = BuildChapterTitle(i, chapter.Name);
            string plainText = RtfHelper.ConvertToPlainText(chapter.Text);

            int matchIndex = plainText.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
            if (matchIndex >= 0)
            {
                int start = Math.Max(0, matchIndex - 40);
                int end = Math.Min(plainText.Length, matchIndex + query.Length + 80);
                string preview = (start > 0 ? "…" : "") + plainText.Substring(start, end - start).Trim() + (end < plainText.Length ? "…" : "");
                int chapterIndex = i;

                yield return new GlobalSearchResultItem(
                    chapterTitle,
                    preview,
                    () => NavigateToChapter(chapterIndex));
            }

            if (chapter.Notes?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                int chapterIndex = i;
                yield return new GlobalSearchResultItem(
                    chapterTitle,
                    chapter.Notes.Substring(0, Math.Min(120, chapter.Notes.Length)),
                    () => NavigateToChapter(chapterIndex));
            }

            if (chapter.Synopsis?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                int chapterIndex = i;
                yield return new GlobalSearchResultItem(
                    chapterTitle,
                    chapter.Synopsis.Substring(0, Math.Min(120, chapter.Synopsis.Length)),
                    () => NavigateToChapter(chapterIndex));
            }
        }
    }

    private IEnumerable<GlobalSearchResultItem> BuildQuickActions()
    {
        yield return new GlobalSearchResultItem(
            _resources.GetString("storyText.Text"),
            string.Empty,
            () => NavigateToPage(NavigationTarget.MainPage));

        yield return new GlobalSearchResultItem(
            _resources.GetString("charactersStory"),
            string.Empty,
            () => NavigateToPage(NavigationTarget.Characters));

        yield return new GlobalSearchResultItem(
            _resources.GetString("shortcutOpenSettings"),
            string.Empty,
            () => NavigateToPage(NavigationTarget.Settings));
    }

    private static string BuildChapterTitle(int index, string chapterName)
    {
        string number = (index + 1).ToString();
        return string.IsNullOrWhiteSpace(chapterName) ? number : $"{number}. {chapterName}";
    }

    /// <summary>Raised when the dialog should close (after navigation).</summary>
    public event Action CloseRequested;

    private void NavigateToPage(NavigationTarget target)
    {
        CloseRequested?.Invoke();
        _navigation.NavigateTo(target);
    }

    public void NavigateToChapter(int index)
    {
        if (index < 0 || index >= _projectState.Chapters.Count)
            return;

        CloseRequested?.Invoke();

        _textEditor.SelectedChapterIndex = index;
        _navigation.NavigateTo(NavigationTarget.MainPage, _projectState.Chapters[index].Token);
    }
}
