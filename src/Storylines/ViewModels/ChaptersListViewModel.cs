using System.Collections.Specialized;

namespace Storylines.ViewModels;

public partial class ChaptersListViewModel : ObservableObject
{
    private readonly ProjectState _projectState;
    private readonly EventAggregator _events;
    private readonly ITextEditorService _textEditor;
    private readonly IChapterWorkflowService _chapterWorkflow;
    private readonly CommandBarViewModel _commandBarViewModel;

    public ObservableCollection<Chapter> Chapters => _projectState.Chapters;

    [ObservableProperty]
    private Chapter _selectedChapter;

    [ObservableProperty]
    private int _selectedIndex = -1;

    [ObservableProperty]
    private bool _canAdd = true;

    [ObservableProperty]
    private Visibility _noChaptersPlaceholderVisibility = Visibility.Visible;

    [ObservableProperty]
    private bool _isExportEnabled;

    [ObservableProperty]
    private bool _isSaveEnabled;

    [ObservableProperty]
    private bool _isSaveCopyEnabled;

    [ObservableProperty]
    private bool _isAddButtonEnabled = true;

    public bool ClosedManually { get; set; }

    public ChaptersListViewModel(
        ProjectState projectState,
        EventAggregator events,
        ITextEditorService textEditor,
        CommandBarViewModel commandBarViewModel,
        IChapterWorkflowService chapterWorkflow)
    {
        _projectState = projectState;
        _events = events;
        _textEditor = textEditor;
        _chapterWorkflow = chapterWorkflow;
        _commandBarViewModel = commandBarViewModel;

        _projectState.Chapters.CollectionChanged += OnChaptersCollectionChanged;
        _projectState.Characters.CollectionChanged += OnCharactersCollectionChanged;

        RefreshState();
    }

    partial void OnSelectedChapterChanged(Chapter value)
    {
        var selectedIndex = value is not null ? _projectState.Chapters.IndexOf(value) : -1;
        if (SelectedIndex != selectedIndex)
            SelectedIndex = selectedIndex;
    }

    partial void OnSelectedIndexChanged(int value)
    {
        var chapter = value >= 0 && value < _projectState.Chapters.Count
            ? _projectState.Chapters[value]
            : null;

        if (!ReferenceEquals(SelectedChapter, chapter))
            SelectedChapter = chapter;

        ApplyChapterSelection(chapter, value);
    }

    partial void OnCanAddChanged(bool value)
    {
        RefreshState();
    }

    private void OnChaptersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (_projectState.Chapters.Count == 0)
        {
            if (SelectedIndex != -1)
            {
                SelectedIndex = -1;
                return;
            }

            RefreshState();
            return;
        }

        if (SelectedIndex >= _projectState.Chapters.Count)
        {
            SelectedIndex = _projectState.Chapters.Count - 1;
            return;
        }

        if (SelectedIndex >= 0)
        {
            var chapter = _projectState.Chapters[SelectedIndex];
            if (!ReferenceEquals(SelectedChapter, chapter))
            {
                SelectedChapter = chapter;
                ApplyChapterSelection(chapter, SelectedIndex);
                return;
            }
        }

        RefreshState();
    }

    private void OnCharactersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        => RefreshState();

    private void ApplyChapterSelection(Chapter chapter, int selectedIndex)
    {
        if (chapter is not null)
        {
            using (TimeTravelChapter.SuppressRecording())
            {
                _textEditor?.LoadChapterContent(chapter);
            }

            _events.Publish(new ChapterSelectedEvent
            {
                SelectedIndex = selectedIndex,
                HasSelection = true
            });
            _events.Publish(new ChapterToolsStateEvent { Enabled = true });
            _textEditor?.Focus();
            _events.Publish(new RefreshNotesPaneEvent());
        }
        else
        {
            _events.Publish(new ChapterSelectedEvent
            {
                SelectedIndex = -1,
                HasSelection = false
            });
            _events.Publish(new ChapterToolsStateEvent { Enabled = false });
        }

        RefreshState();
    }

    private void RefreshState()
    {
        NoChaptersPlaceholderVisibility = _projectState.Chapters.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        IsExportEnabled = _projectState.Chapters.Count > 0 || _projectState.Characters.Count > 0;
        IsSaveEnabled = _projectState.Chapters.Count > 0;
        IsSaveCopyEnabled = _projectState.Chapters.Count > 0;
        IsAddButtonEnabled = CanAdd;

        _commandBarViewModel.IsExportEnabled = IsExportEnabled;
        _commandBarViewModel.IsSaveEnabled = IsSaveEnabled;
        _commandBarViewModel.IsSaveCopyEnabled = IsSaveCopyEnabled;
        _commandBarViewModel.IsChapterAddEnabled = IsAddButtonEnabled;
    }

    [RelayCommand]
    private void OpenCreateChapterDialog()
    {
        _chapterWorkflow.OpenCreateChapterDialog();
    }

    [RelayCommand]
    private void CloseChapterList()
    {
        _events.Publish(new ToggleChapterListEvent
        {
            Open = false,
            Manually = true
        });
    }

    public void OpenRenameChapterDialog(string token, bool doubleTap = false)
        => _chapterWorkflow.OpenRenameChapterDialog(token, doubleTap);

    public void DeleteChapter(string token)
        => _chapterWorkflow.DeleteChapter(token);

    public void DuplicateChapter(string token)
        => _chapterWorkflow.DuplicateChapter(token);

    public void OpenChapterTagsDialog(string token)
        => _chapterWorkflow.OpenChapterTagsDialog(token);

    public void SetChapterStatus(string token, ChapterStatus status)
        => _chapterWorkflow.SetChapterStatus(token, status);

    public void ReorderChapter(string token, int newPosition, int oldPosition)
        => _chapterWorkflow.ReorderChapter(token, newPosition, oldPosition);
}
