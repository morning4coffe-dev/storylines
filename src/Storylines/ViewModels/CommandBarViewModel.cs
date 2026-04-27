using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using Windows.System;

namespace Storylines.ViewModels
{
    public partial class CommandBarViewModel : ObservableObject
    {
        private readonly IDialogService _dialogs;
        private readonly ITextEditorService _textEditor;

        [ObservableProperty]
        private bool _canUndo;

        [ObservableProperty]
        private bool _canRedo;

        [ObservableProperty]
        private bool _isExportEnabled;

        [ObservableProperty]
        private bool _isSaveEnabled;

        [ObservableProperty]
        private bool _isSaveCopyEnabled;

        [ObservableProperty]
        private bool _isChapterAddEnabled = true;

        [ObservableProperty]
        private bool _isCharactersEnabled = true;

        [ObservableProperty]
        private bool _isAutosaveChecked;

        public CommandBarViewModel(IDialogService dialogs = null, EventAggregator events = null, ITextEditorService textEditor = null)
        {
            _dialogs = dialogs ?? App.TryGetService<IDialogService>() ?? new DialogService();
            _textEditor = textEditor ?? App.TryGetService<ITextEditorService>();
            IsAutosaveChecked = SettingsValues.autosaveEnabled;

            (events ?? App.TryGetService<EventAggregator>() ?? new EventAggregator())
                .Subscribe<UndoRedoStateChangedEvent>(OnUndoRedoStateChanged);
        }

        private void OnUndoRedoStateChanged(UndoRedoStateChangedEvent e)
        {
            if (e.Context == "chapters")
            {
                CanUndo = e.CanUndo;
                CanRedo = e.CanRedo;
            }
        }

        [RelayCommand]
        private void Undo() => TimeTravelChapter.Undo();

        [RelayCommand]
        private void Redo() => TimeTravelChapter.Redo();

        [RelayCommand]
        private void Save() => SaveSystem.Save();

        [RelayCommand]
        private void SaveCopy() => SaveSystem.SaveCopy();

        [RelayCommand]
        private void Load() => _dialogs.OpenLoadDialogue();

        [RelayCommand]
        private void Export() => _dialogs.OpenExportDialogue();

        [RelayCommand]
        private void ToggleAutosave()
        {
            if (IsAutosaveChecked)
            {
                if (SaveSystem.currentProject?.file == null)
                {
                    // Can't autosave without a file — revert the toggle
                    IsAutosaveChecked = false;
                    return;
                }
                AutosaveService.Enable();
            }
            else
                AutosaveService.Disable();
        }

        [RelayCommand]
        private void OpenFocusMode() => _dialogs.OpenFocusMode();

        [RelayCommand]
        private void OpenReadOnlyMode() => _dialogs.OpenModePicker("readonly");

        [RelayCommand]
        private void ShowProjectStats() => _dialogs.OpenProjectStats(false);

        [RelayCommand]
        private void NavigateToCharacters() => AppView.current.ChangePage(AppView.Pages.Characters);

        [RelayCommand]
        private void NavigateToBranchingDialogue()
        {
            if (SettingsValues.experimentalFeaturesEnabled)
            {
                // Pass current chapter token so the dialogue page opens with context
                var projectState = App.TryGetService<ProjectState>();
                string chapterToken = null;
                if (projectState?.Chapters != null && _textEditor != null)
                {
                    var idx = _textEditor.SelectedChapterIndex;
                    if (idx >= 0 && idx < projectState.Chapters.Count)
                        chapterToken = projectState.Chapters[idx].Token;
                }

                var nav = App.TryGetService<INavigationService>();
                nav?.NavigateTo(NavigationTarget.BranchingDialogue, chapterToken);
            }
        }

        [RelayCommand]
        private void ShowShortcuts() => _dialogs.OpenShortcuts();

        [RelayCommand]
        private async void OpenFeedback()
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/morning4coffe-dev/Storylines/issues/new"));
        }
    }
}
