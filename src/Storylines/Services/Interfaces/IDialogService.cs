using Storylines.Models;

namespace Storylines.Services.Interfaces
{
    public interface IDialogService
    {
        void OpenSaveDialogue();
        void OpenSaveCopyDialogue();
        void OpenLoadDialogue();
        void OpenExportDialogue(ExportTarget target = ExportTarget.None);
        void OpenChapterCreator();
        void OpenChapterRenamer(Chapter chapter, bool doubleTap = false);
        void OpenChapterTags(Chapter chapter);
        void OpenFocusMode();
        void OpenModePicker(string preselect = "focus");
        void OpenProjectStats(bool showInDownBar);
        void OpenProjectFileInfo();
        void OpenShortcuts();

        /// <summary>Clears the editor and all project state in the shell.</summary>
        void ClearEverything();

        /// <summary>Dismisses the load-project dialogue if it is open.</summary>
        void DismissLoadDialogue();
    }
}
