using Storylines.Models;

namespace Storylines.Services.Interfaces
{
    public interface IChapterWorkflowService
    {
        void OpenCreateChapterDialog();
        void OpenRenameChapterDialog(string chapterToken, bool doubleTap = false);
        void CreateChapterFromInput(string enteredName);
        void RenameChapter(string chapterToken, string newName);
        void DeleteChapter(string chapterToken);
        void OpenChapterTagsDialog(string chapterToken);
        void SetChapterStatus(string chapterToken, ChapterStatus status);
        void ReorderChapter(string chapterToken, int newPosition, int previousPosition);
    }
}