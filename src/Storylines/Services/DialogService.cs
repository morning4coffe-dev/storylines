using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Services.Interfaces;
using Storylines.Models;

namespace Storylines.Services
{
    public class DialogService : IDialogService
    {
        public void OpenSaveDialogue() => SaveDialogue.Open(SaveDialogue.Type.Save);

        public void OpenSaveCopyDialogue() => SaveDialogue.Open(SaveDialogue.Type.SaveCopy);

        public void OpenLoadDialogue()
        {
            LoadProjectDialogue.Open(AppView.current.XamlRoot);
        }

        public void OpenExportDialogue(ExportTarget target = ExportTarget.None) => ExportDialogue.Open(target);

        public void OpenChapterCreator() => ChapterCreatorOrRenamer.Open(null, false);

        public void OpenChapterRenamer(Chapter chapter, bool doubleTap = false) => ChapterCreatorOrRenamer.Open(chapter, doubleTap);

        public void OpenChapterTags(Chapter chapter) => ChapterTagsDialogue.Open(chapter);

        public void OpenFocusMode() => ModePickerDialogue.Open("focus");
        public void OpenModePicker(string preselect = "focus") => ModePickerDialogue.Open(preselect);

        public void OpenProjectStats(bool showInDownBar) => ProjectStatsDialogue.Open(showInDownBar);
        public void OpenProjectFileInfo() => ProjectFileInfoDialogue.Open();

        public void OpenShortcuts() => ShortcutsDialogue.Open();

        public void ClearEverything()
        {
            AppView.current?.ClearEverything();
        }

        public void DismissLoadDialogue()
        {
            if (LoadProjectDialogue.loadFile != null)
            {
                LoadProjectDialogue.loadFile.isEscape = false;
                LoadProjectDialogue.loadFile.Hide();
            }
        }
    }
}
