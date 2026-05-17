using Microsoft.UI.Text;

namespace Storylines.Services;

public sealed class ChapterWorkflowService : IChapterWorkflowService
{
    private readonly IDialogService _dialogs;
    private readonly ProjectState _projectState;
    private readonly ResourceLoader _resources;
    private readonly ITextEditorService _textEditor;

    public ChapterWorkflowService(
        IDialogService dialogs,
        ProjectState projectState,
        ITextEditorService textEditor)
    {
        _dialogs = dialogs;
        _projectState = projectState;
        _resources = ResourceLoader.GetForViewIndependentUse();
        _textEditor = textEditor;
    }

    public void OpenCreateChapterDialog() => _dialogs.OpenChapterCreator();

    public void OpenRenameChapterDialog(string chapterToken, bool doubleTap = false)
    {
        var chapter = FindChapter(chapterToken);
        if (chapter is not null)
            _dialogs.OpenChapterRenamer(chapter, doubleTap);
    }

    public void CreateChapterFromInput(string enteredName)
    {
        var nextIndex = _projectState.Chapters.Count;
        _projectState.AddChapterFromCreator(_projectState.Chapters.Count + 1, enteredName ?? string.Empty);

        SelectChapter(nextIndex);
    }

    public void CreateChapterWithContent(string enteredName, string initialText)
    {
        var nextIndex = _projectState.Chapters.Count;
        _projectState.AddChapterFromCreator(_projectState.Chapters.Count + 1, enteredName ?? string.Empty);

        var createdChapter = nextIndex >= 0 && nextIndex < _projectState.Chapters.Count
            ? _projectState.Chapters[nextIndex]
            : null;

        if (createdChapter is not null && !string.IsNullOrWhiteSpace(initialText))
            createdChapter.Text = ConvertPlainTextToRtf(initialText);

        SelectChapter(nextIndex);

        if (_textEditor is not null && createdChapter is not null)
            _textEditor.LoadChapterContent(createdChapter);
    }

    public void RenameChapter(string chapterToken, string newName)
    {
        var chapter = FindChapter(chapterToken);
        if (chapter is not null)
            _projectState.RenameChapter(chapter.Token, newName);
    }

    public void DeleteChapter(string chapterToken)
    {
        if (!string.IsNullOrWhiteSpace(chapterToken))
            _projectState.RemoveChapter(chapterToken);
    }

    public void DuplicateChapter(string chapterToken)
    {
        var chapter = FindChapter(chapterToken);
        if (chapter is null)
            return;

        var duplicate = _projectState.DuplicateChapter(chapter.Token, BuildDuplicateChapterName(chapter.Name));
        if (duplicate is null)
            return;

        SelectChapter(_projectState.FindChapterID(duplicate.Token));
    }

    public void OpenChapterTagsDialog(string chapterToken)
    {
        var chapter = FindChapter(chapterToken);
        if (chapter is not null)
            _dialogs.OpenChapterTags(chapter);
    }

    public void SetChapterStatus(string chapterToken, ChapterStatus status)
    {
        var chapter = FindChapter(chapterToken);
        if (chapter is null || chapter.Status == status)
            return;

        chapter.Status = status;
        TimeTravelSystem.SomethingChanged();
    }

    public void ReorderChapter(string chapterToken, int newPosition, int previousPosition)
    {
        if (!string.IsNullOrWhiteSpace(chapterToken))
            _projectState.ReorderChapter(chapterToken, newPosition, previousPosition);
    }

    private Chapter FindChapter(string chapterToken)
    {
        return string.IsNullOrWhiteSpace(chapterToken)
            ? null
            : _projectState.FindChapter(chapterToken);
    }

    private void SelectChapter(int chapterIndex)
    {
        if (_textEditor is not null)
            _textEditor.SelectedChapterIndex = chapterIndex;
    }

    private string BuildDuplicateChapterName(string chapterName)
    {
        var baseFormat = _resources.GetString("duplicateChapterNameFormat");
        if (string.IsNullOrWhiteSpace(baseFormat))
            baseFormat = "{0} (Copy)";

        var candidate = string.Format(baseFormat, chapterName ?? string.Empty);
        if (IsChapterNameAvailable(candidate))
            return candidate;

        var indexedFormat = _resources.GetString("duplicateChapterNameIndexedFormat");
        if (string.IsNullOrWhiteSpace(indexedFormat))
            indexedFormat = "{0} (Copy {1})";

        var suffixIndex = 2;
        do
        {
            candidate = string.Format(indexedFormat, chapterName ?? string.Empty, suffixIndex);
            suffixIndex++;
        }
        while (!IsChapterNameAvailable(candidate));

        return candidate;
    }

    private bool IsChapterNameAvailable(string candidate)
    {
        return !_projectState.Chapters.Any(existing =>
            string.Equals(existing?.Name, candidate, StringComparison.CurrentCultureIgnoreCase));
    }

    private static string ConvertPlainTextToRtf(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var richEditBox = new RichEditBox();
        richEditBox.Document.SetText(TextSetOptions.None, text);
        richEditBox.Document.GetText(TextGetOptions.FormatRtf, out var rtfText);
        return rtfText ?? string.Empty;
    }
}