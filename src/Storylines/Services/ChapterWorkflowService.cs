using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services.Interfaces;

namespace Storylines.Services
{
    public sealed class ChapterWorkflowService : IChapterWorkflowService
    {
        private readonly IDialogService _dialogs;
        private readonly ProjectState _projectState;
        private readonly ITextEditorService _textEditor;

        public ChapterWorkflowService(
            IDialogService dialogs = null,
            ProjectState projectState = null,
            ITextEditorService textEditor = null)
        {
            _dialogs = dialogs ?? App.TryGetService<IDialogService>() ?? new DialogService();
            _projectState = projectState ?? App.TryGetService<ProjectState>() ?? new ProjectState();
            _textEditor = textEditor ?? App.TryGetService<ITextEditorService>();
        }

        public void OpenCreateChapterDialog() => _dialogs.OpenChapterCreator();

        public void OpenRenameChapterDialog(string chapterToken, bool doubleTap = false)
        {
            var chapter = FindChapter(chapterToken);
            if (chapter != null)
                _dialogs.OpenChapterRenamer(chapter, doubleTap);
        }

        public void CreateChapterFromInput(string enteredName)
        {
            var nextIndex = _projectState.Chapters.Count;
            _projectState.AddChapterFromCreator(_projectState.Chapters.Count + 1, enteredName ?? string.Empty);

            if (_textEditor != null)
                _textEditor.SelectedChapterIndex = nextIndex;
        }

        public void RenameChapter(string chapterToken, string newName)
        {
            var chapter = FindChapter(chapterToken);
            if (chapter != null)
                _projectState.RenameChapter(chapter.Token, newName);
        }

        public void DeleteChapter(string chapterToken)
        {
            if (!string.IsNullOrWhiteSpace(chapterToken))
                _projectState.RemoveChapter(chapterToken);
        }

        public void OpenChapterTagsDialog(string chapterToken)
        {
            var chapter = FindChapter(chapterToken);
            if (chapter != null)
                _dialogs.OpenChapterTags(chapter);
        }

        public void SetChapterStatus(string chapterToken, ChapterStatus status)
        {
            var chapter = FindChapter(chapterToken);
            if (chapter == null || chapter.Status == status)
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
    }
}