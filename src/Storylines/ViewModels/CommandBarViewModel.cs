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
        private readonly IProjectPersistenceService _persistence;
        private readonly ITextEditorService _textEditor;
        private readonly ProjectState _projectState;
        private readonly INavigationService _navigation;

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

        public CommandBarViewModel(
            IDialogService dialogs,
            EventAggregator events,
            ITextEditorService textEditor,
            IProjectPersistenceService persistence,
            ProjectState projectState,
            INavigationService navigation)
        {
            _dialogs = dialogs;
            _persistence = persistence;
            _textEditor = textEditor;
            _projectState = projectState;
            _navigation = navigation;
            IsAutosaveChecked = SettingsValues.autosaveEnabled;

            events.Subscribe<UndoRedoStateChangedEvent>(OnUndoRedoStateChanged);
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
        private void Save() => _persistence.Save();

        [RelayCommand]
        private void SaveCopy() => _persistence.SaveCopy();

        [RelayCommand]
        private void Load() => _dialogs.OpenLoadDialogue();

        [RelayCommand]
        private void Export() => _dialogs.OpenExportDialogue();

        [RelayCommand]
        private void ToggleAutosave()
        {
            if (IsAutosaveChecked)
                _persistence.EnableAutosave();
            else
                _persistence.DisableAutosave();
        }

        [RelayCommand]
        private void OpenFocusMode() => _dialogs.OpenFocusMode();

        [RelayCommand]
        private void OpenReadOnlyMode() => _dialogs.OpenModePicker("readonly");

        [RelayCommand]
        private void ShowProjectStats() => _dialogs.OpenProjectStats(false);

        [RelayCommand]
        private void ShowProjectFileInfo() => _dialogs.OpenProjectFileInfo();

        [RelayCommand]
        private void NavigateToCharacters() => AppView.current.ChangePage(AppView.Pages.Characters);

        [RelayCommand]
        private void ShowShortcuts() => _dialogs.OpenShortcuts();

        [RelayCommand]
        private async System.Threading.Tasks.Task OpenFeedbackAsync()
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/morning4coffe-dev/Storylines/issues/new"));
        }
    }
}
