using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Scripts.Services.Interfaces;
using Storylines.Scripts.Variables;

namespace Storylines.Scripts.Services
{
    public class DialogService : IDialogService
    {
        public void OpenSaveDialogue() => SaveDialogue.Open(SaveDialogue.Type.Save);

        public void OpenSaveCopyDialogue() => SaveSystem.SaveCopy();

        public void OpenLoadDialogue()
        {
            LoadProjectDialogue.Open();
        }

        public void OpenExportDialogue() => ExportDialogue.Open(default);

        public void OpenChapterCreator() => ChapterCreatorOrRenamer.Open(null, false);

        public void OpenChapterRenamer(Chapter chapter, bool doubleTap = false) => ChapterCreatorOrRenamer.Open(chapter, doubleTap);

        public void OpenFocusMode() => ModesDialogue.Open();

        public void OpenProjectStats(bool showInDownBar) => ProjectStatsDialogue.Open(showInDownBar);

        public void OpenShortcuts() => ShortcutsDialogue.Open();
    }
}
