namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the ChapterNotesPane control.
/// Manages chapter notes, synopsis, location, and plot threads — previously in code-behind.
/// </summary>
public partial class ChapterNotesPaneViewModel : ObservableObject
{
    private readonly ProjectState _projectState;
    private readonly ITextEditorService _textEditor;
    private readonly EventAggregator _events;
    private bool _isUpdating;

    [ObservableProperty] private string _notesText = string.Empty;
    [ObservableProperty] private string _synopsisText = string.Empty;
    [ObservableProperty] private string _locationText = string.Empty;
    [ObservableProperty] private string _plotThreadsText = string.Empty;
    [ObservableProperty] private bool _isFieldsEnabled;

    public ChapterNotesPaneViewModel(
        ProjectState projectState,
        ITextEditorService textEditor,
        EventAggregator events)
    {
        _projectState = projectState;
        _textEditor = textEditor;
        _events = events;

        _events.Subscribe<ChapterSelectedEvent>(e => LoadNotes());
        _events.Subscribe<RefreshNotesPaneEvent>(_ => LoadNotes());
    }

    /// <summary>
    /// Loads notes fields from the currently selected chapter.
    /// </summary>
    public void LoadNotes()
    {
        _isUpdating = true;

        var chapter = GetSelectedChapter();

        if (chapter is not null)
        {
            NotesText = chapter.Notes ?? string.Empty;
            SynopsisText = chapter.Synopsis ?? string.Empty;
            LocationText = chapter.Location ?? string.Empty;
            PlotThreadsText = chapter.PlotThreads?.Count > 0 ? string.Join(", ", chapter.PlotThreads) : string.Empty;
            IsFieldsEnabled = true;
        }
        else
        {
            NotesText = string.Empty;
            SynopsisText = string.Empty;
            LocationText = string.Empty;
            PlotThreadsText = string.Empty;
            IsFieldsEnabled = false;
        }

        _isUpdating = false;
    }

    partial void OnNotesTextChanged(string value)
    {
        if (_isUpdating) return;
        var chapter = GetSelectedChapter();
        if (chapter is not null)
        {
            chapter.Notes = value;
            TimeTravelSystem.SomethingChanged();
        }
    }

    partial void OnSynopsisTextChanged(string value)
    {
        if (_isUpdating) return;
        var chapter = GetSelectedChapter();
        if (chapter is not null)
        {
            chapter.Synopsis = value;
            TimeTravelSystem.SomethingChanged();
        }
    }

    partial void OnLocationTextChanged(string value)
    {
        if (_isUpdating) return;
        var chapter = GetSelectedChapter();
        if (chapter is not null)
        {
            chapter.Location = value;
            TimeTravelSystem.SomethingChanged();
        }
    }

    partial void OnPlotThreadsTextChanged(string value)
    {
        if (_isUpdating) return;
        var chapter = GetSelectedChapter();
        if (chapter is not null)
        {
            chapter.PlotThreads = (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            // Auto-register new plot threads in the project
            foreach (var thread in chapter.PlotThreads)
            {
                if (!_projectState.PlotThreads.Contains(thread, StringComparer.CurrentCultureIgnoreCase))
                    _projectState.PlotThreads.Add(thread);
            }

            TimeTravelSystem.SomethingChanged();
        }
    }

    private Chapter GetSelectedChapter()
    {
        var index = _textEditor.SelectedChapterIndex;
        if (index < 0 || index >= _projectState.Chapters.Count)
            return null;
        return _projectState.Chapters[index];
    }
}
