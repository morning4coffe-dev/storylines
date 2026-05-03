using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Services.Interfaces;
using Storylines.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using System;

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
            return await ShowAsync(
                dialog,
                new DialogShowOptions
                {
                    CloseCurrentDialog = closeCurrentDialog,
                });
        }

        public async Task<ContentDialogResult> ShowAsync(ContentDialog dialog, DialogShowOptions options)
        {
            if (dialog == null)
                return ContentDialogResult.None;

            options ??= DialogShowOptions.Default;

            if (options.CloseCurrentDialog && _windowContext.CurrentDialog != null && !ReferenceEquals(_windowContext.CurrentDialog, dialog))
                HideCurrentDialog();

            var xamlRoot = await ResolveXamlRootAsync(options);
            if (xamlRoot != null)
                dialog.XamlRoot = xamlRoot;

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

        public async Task<ContentDialogResult> ShowMessageAsync(DialogDefinition definition, DialogShowOptions options = null)
        {
            if (definition == null)
                return ContentDialogResult.None;

            var dialog = new StorylinesContentDialog
            {
                Title = definition.Title,
                Content = definition.Content,
                PrimaryButtonText = definition.PrimaryButtonText,
                SecondaryButtonText = definition.SecondaryButtonText,
                CloseButtonText = definition.CloseButtonText,
                DefaultButton = definition.DefaultButton,
                IsPrimaryButtonEnabled = definition.IsPrimaryButtonEnabled,
            };

            return await ShowAsync(dialog, options);
        }

        public void HideCurrentDialog()
        {
            if (_windowContext.CurrentDialog is LoadProjectDialogue loadProjectDialog)
                loadProjectDialog.isEscape = false;

            _windowContext.CurrentDialog?.Hide();
        }

        private async Task<XamlRoot> ResolveXamlRootAsync(DialogShowOptions options)
        {
            if (options?.XamlRootOverride != null)
                return options.XamlRootOverride;

            if (_windowContext.XamlRoot != null || options == null || !options.WaitForXamlRoot)
                return _windowContext.XamlRoot;

            var waited = 0;
            var timeout = Math.Max(0, options.XamlRootWaitTimeoutMs);
            while (_windowContext.XamlRoot == null && waited < timeout)
            {
                await Task.Delay(50);
                waited += 50;
            }

            return _windowContext.XamlRoot;
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
            HideCurrentDialog();
        }
    }
}
