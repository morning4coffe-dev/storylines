using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Views.Controls;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using Windows.System;

namespace Storylines.ViewModels
{
    public partial class CommandBarViewModel : ObservableObject
    {
        private readonly IDialogService _dialogs;

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

        public CommandBarViewModel()
        {
            _dialogs = ServiceLocator.Dialogs;
            IsAutosaveChecked = SettingsValues.autosaveEnabled;

            ServiceLocator.Events.Subscribe<UndoRedoStateChangedEvent>(OnUndoRedoStateChanged);
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
                AutosaveService.Enable();
            else
                AutosaveService.Disable();
        }

        [RelayCommand]
        private void OpenFocusMode() => _dialogs.OpenFocusMode();

        [RelayCommand]
        private void ShowProjectStats() => _dialogs.OpenProjectStats(false);

        [RelayCommand]
        private void NavigateToCharacters() => ServiceLocator.Navigation.NavigateTo(NavigationTarget.Characters);

        [RelayCommand]
        private void ShowShortcuts() => _dialogs.OpenShortcuts();

        [RelayCommand]
        private async void OpenFeedback()
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/morning4coffe-dev/Storylines/issues/new"));
        }
    }
}
