using Storylines.Models;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Storylines.Services.Interfaces
{
    public interface IDialogService
    {
        ContentDialog CurrentDialog { get; }
        Task<ContentDialogResult> ShowAsync(ContentDialog dialog, bool closeCurrentDialog = true);
        Task<ContentDialogResult> ShowAsync(ContentDialog dialog, DialogShowOptions options);
        Task<ContentDialogResult> ShowMessageAsync(DialogDefinition definition, DialogShowOptions options = null);
        void HideCurrentDialog();
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

        /// <summary>Shows the "unsaved progress" dialog and handles Save/Don't-Save/Cancel.</summary>
        Task ShowUnsavedProgressDialogueAsync(bool appClosing);

        /// <summary>Shows a confirmation dialog when leaving focus mode before the goal is met.</summary>
        Task ShowFocusModeLeaveDialogueAsync();

        /// <summary>Shows the "unapplied character changes" dialog and handles Apply/Discard/Cancel.</summary>
        Task ShowUnappliedCharacterChangesDialogueAsync();

        /// <summary>Shows the "no characters" dialog and navigates to add one if confirmed.</summary>
        Task ShowNoCharactersDialogueAsync();
    }
}
