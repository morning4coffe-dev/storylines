using Storylines.Scripts.Variables;

namespace Storylines.Scripts.Services.Interfaces
{
    public interface IDialogService
    {
        void OpenSaveDialogue();
        void OpenSaveCopyDialogue();
        void OpenLoadDialogue();
        void OpenExportDialogue();
        void OpenChapterCreator();
        void OpenChapterRenamer(Chapter chapter, bool doubleTap = false);
        void OpenFocusMode();
        void OpenProjectStats(bool showInDownBar);
        void OpenShortcuts();
    }
}
