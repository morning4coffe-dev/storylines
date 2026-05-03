using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Services.Interfaces;
using Storylines.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace Storylines.Services
{
    public class DialogService : IDialogService
    {
        private readonly WindowContext _windowContext;

        public DialogService(WindowContext windowContext)
        {
            _windowContext = windowContext;
        }

        public ContentDialog CurrentDialog => _windowContext.CurrentDialog;

        public async Task<ContentDialogResult> ShowAsync(ContentDialog dialog, bool closeCurrentDialog = true)
        {
            if (dialog == null)
                return ContentDialogResult.None;

            if (closeCurrentDialog && _windowContext.CurrentDialog != null)
                _windowContext.CurrentDialog.Hide();

            dialog.XamlRoot ??= _windowContext.XamlRoot;
            dialog.RequestedTheme = _windowContext.AppView?.ActualTheme ?? ElementTheme.Default;
            _windowContext.CurrentDialog = dialog;

            try
            {
                return await dialog.ShowAsync();
            }
            finally
            {
                if (_windowContext.CurrentDialog == dialog)
                    _windowContext.CurrentDialog = null;
            }
        }

        public void OpenSaveDialogue() => SaveDialogue.Open(SaveDialogue.Type.Save);

        public void OpenSaveCopyDialogue() => SaveDialogue.Open(SaveDialogue.Type.SaveCopy);

        public void OpenLoadDialogue()
        {
            LoadProjectDialogue.Open(_windowContext.XamlRoot);
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
            _windowContext.AppView?.ClearEverything();
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
