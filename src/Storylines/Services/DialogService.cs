using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Services.Interfaces;
using Storylines.Services.Modes;
using Storylines.Helpers;
using Storylines.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using System;
using Windows.ApplicationModel.Resources;

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
            if (dialog is null)
                return ContentDialogResult.None;

            options ??= DialogShowOptions.Default;

            if (options.CloseCurrentDialog && _windowContext.CurrentDialog is not null && !ReferenceEquals(_windowContext.CurrentDialog, dialog))
                HideCurrentDialog();

            var xamlRoot = await ResolveXamlRootAsync(options);
            if (xamlRoot is not null)
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
            if (definition is null)
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
            if (options?.XamlRootOverride is not null)
                return options.XamlRootOverride;

            if (_windowContext.XamlRoot is not null || options is null || !options.WaitForXamlRoot)
                return _windowContext.XamlRoot;

            var waited = 0;
            var timeout = Math.Max(0, options.XamlRootWaitTimeoutMs);
            while (_windowContext.XamlRoot is null && waited < timeout)
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

        // ── Confirmation dialogues ────────────────────────────────────

        public async Task ShowUnsavedProgressDialogueAsync(bool appClosing)
        {
            var resources = ResourceLoader.GetForViewIndependentUse();
            var persistence = App.GetService<IProjectPersistenceService>();

            var result = await ShowMessageAsync(new DialogDefinition
            {
                Title = resources.GetString("exitWithoutSaveDialogTitle"),
                Content = resources.GetString("exitWithoutSaveDialogDescription"),
                PrimaryButtonText = resources.GetString("exitWithoutSaveDialogSave"),
                SecondaryButtonText = resources.GetString("exitWithoutSaveDialogDontSave"),
                CloseButtonText = resources.GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
            });

            switch (result)
            {
                case ContentDialogResult.Primary:
                    persistence.SaveAndExitOrClearAll(appClosing);
                    break;
                case ContentDialogResult.Secondary:
                    await RecoveryService.ClearRecoveryDataAsync();

                    if (appClosing)
                    {
                        App.GetService<IUndoRedoService>().MarkClean();
                        App.GetService<IWindowManager>().Close(_windowContext);
                    }
                    else
                    {
                        if (persistence.CurrentProject is not null)
                            persistence.CurrentProject.file = null;
                        _windowContext.AppView?.ClearEverything();
                        TimeTravelSystem.unSavedProgress = false;

                        OpenLoadDialogue();
                    }
                    break;
            }
        }

        public async Task ShowFocusModeLeaveDialogueAsync()
        {
            var resources = ResourceLoader.GetForViewIndependentUse();
            var result = await ShowMessageAsync(new DialogDefinition
            {
                Title = resources.GetString("FocusModeLeaveDialogueTitle"),
                Content = resources.GetString("FocusModeLeaveDialogueDescription"),
                PrimaryButtonText = resources.GetString("FocusModeLeaveDialogueStay"),
                SecondaryButtonText = resources.GetString("FocusModeLeaveDialogueLeave"),
                DefaultButton = ContentDialogButton.Primary,
            });

            if (result == ContentDialogResult.Secondary)
                App.TryGetService<EditorModeService>()?.Deactivate();
        }

        public async Task ShowUnappliedCharacterChangesDialogueAsync()
        {
            var resources = ResourceLoader.GetForViewIndependentUse();
            var result = await ShowMessageAsync(new DialogDefinition
            {
                Title = resources.GetString("changesCharactersPageDialogueTitle"),
                Content = resources.GetString("changesCharactersPageDialogueDescription"),
                PrimaryButtonText = resources.GetString("changesCharactersPageDialogueApplyChanges"),
                SecondaryButtonText = resources.GetString("changesCharactersPageDialogueDontApplyChanges"),
                CloseButtonText = resources.GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
            });

            switch (result)
            {
                case ContentDialogResult.Primary:
                    _windowContext.CharactersPage?.ApplyChanges();
                    _windowContext.AppView?.GoBack();
                    break;
                case ContentDialogResult.Secondary:
                    _windowContext.CharactersPage?.CancelEdit();
                    _windowContext.AppView?.GoBack();
                    break;
            }
        }

        public async Task ShowNoCharactersDialogueAsync()
        {
            var resources = ResourceLoader.GetForViewIndependentUse();
            var result = await ShowMessageAsync(new DialogDefinition
            {
                Title = resources.GetString("noCharactersDialogueTitle"),
                Content = resources.GetString("noCharactersDialogueDescription"),
                PrimaryButtonText = resources.GetString("noCharactersDialogueAddNew"),
                CloseButtonText = resources.GetString("exitWithoutSaveDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
            });

            if (result == ContentDialogResult.Primary)
            {
                _windowContext.AppView?.ChangePage(AppView.Pages.Characters);
                _windowContext.CharactersPage?.Add();
            }
        }
    }
}
