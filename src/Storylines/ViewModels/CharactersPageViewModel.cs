using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Storylines.ViewModels
{
    public partial class CharactersPageViewModel : ObservableObject
    {
        private readonly ProjectState _projectState;
        private readonly INavigationService _navigation;

        public ObservableCollection<Character> Characters => _projectState.Characters;

        [ObservableProperty]
        private Character _selectedCharacter;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _isAddEnabled = true;

        [ObservableProperty]
        private bool _isRemoveEnabled = true;

        [ObservableProperty]
        private string _editButtonLabel;

        [ObservableProperty]
        private string _editButtonGlyph = "\uE104";

        [ObservableProperty]
        private Visibility _cancelButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private bool _isListEnabled = true;

        [ObservableProperty]
        private bool _isFieldsEnabled;

        [ObservableProperty]
        private string _nameText = string.Empty;

        [ObservableProperty]
        private string _descriptionText = string.Empty;

        [ObservableProperty]
        private string _roleText = string.Empty;

        [ObservableProperty]
        private string _ageText = string.Empty;

        [ObservableProperty]
        private string _traitsText = string.Empty;

        [ObservableProperty]
        private string _appearanceText = string.Empty;

        [ObservableProperty]
        private BitmapImage _profilePicture;

        private CharacterPicture _pictureData;

        [ObservableProperty]
        private bool _isCharacterSelected;

        [ObservableProperty]
        private bool _canUndo;

        [ObservableProperty]
        private bool _canRedo;

        [ObservableProperty]
        private string _dialogueNodeCountText;

        public bool UnappliedChanges { get; set; }

        private Character _characterBeforeChange;

        public CharactersPageViewModel(
            ProjectState projectState,
            EventAggregator events,
            INavigationService navigation)
        {
            _projectState = projectState;
            _navigation = navigation;
            EditButtonLabel = ResourceLoader.GetForViewIndependentUse().GetString("editText");

            events.Subscribe<UndoRedoStateChangedEvent>(OnUndoRedoStateChanged);
        }

        private void OnUndoRedoStateChanged(UndoRedoStateChangedEvent e)
        {
            if (e.Context == "characters")
            {
                CanUndo = e.CanUndo;
                CanRedo = e.CanRedo;
            }
        }

        partial void OnSelectedCharacterChanged(Character value)
        {
            IsCharacterSelected = value is not null;
            if (value is not null)
            {
                NameText = value.Name ?? string.Empty;
                DescriptionText = value.Description ?? string.Empty;
                RoleText = value.Role ?? string.Empty;
                AgeText = value.Age ?? string.Empty;
                TraitsText = value.TraitsText ?? string.Empty;
                AppearanceText = value.Appearance ?? string.Empty;
                ProfilePicture = value.Picture?.Image;
                _pictureData = value.Picture;
                UpdateDialogueNodeCount(value);
            }
        }

        private void UpdateDialogueNodeCount(Character character)
        {
            DialogueNodeCountText = null;
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            if (IsEditMode)
            {
                // Leaving edit mode - apply changes if something changed
                if (SelectedCharacter is not null && DidSomethingChange())
                {
                    ApplyChanges();
                    SortCharacters();
                }
                ExitEditMode();
            }
            else
            {
                EnterEditMode();
            }
        }

        public void EnterEditMode()
        {
            if (SelectedCharacter is null) return;

            IsEditMode = true;
            IsFieldsEnabled = true;
            IsListEnabled = false;

            _characterBeforeChange = _projectState.CopyCharacter(SelectedCharacter.Token);

            CancelButtonVisibility = Visibility.Collapsed;
            EditButtonLabel = ResourceLoader.GetForViewIndependentUse().GetString("cancelText");
            EditButtonGlyph = "\uE10A";
            UnappliedChanges = false;
        }

        public void ExitEditMode()
        {
            IsEditMode = false;
            IsFieldsEnabled = false;
            IsListEnabled = true;

            CancelButtonVisibility = Visibility.Collapsed;
            EditButtonLabel = ResourceLoader.GetForViewIndependentUse().GetString("editText");
            EditButtonGlyph = "\uE104";
            UnappliedChanges = false;
        }

        public void MarkUnappliedChanges()
        {
            UnappliedChanges = true;
            CancelButtonVisibility = Visibility.Visible;
            EditButtonLabel = ResourceLoader.GetForViewIndependentUse().GetString("applyChanges");
            EditButtonGlyph = "\uE081";
        }

        public void MarkCleanEditMode()
        {
            UnappliedChanges = false;
            CancelButtonVisibility = Visibility.Collapsed;
            EditButtonLabel = ResourceLoader.GetForViewIndependentUse().GetString("cancelText");
            EditButtonGlyph = "\uE10A";
        }

        public void ApplyChanges()
        {
            if (SelectedCharacter is not null && _characterBeforeChange is not null)
            {
                TimeTravelCharacter.RecordChanged(_characterBeforeChange);
                UnappliedChanges = false;

                SelectedCharacter.Name = NameText;
                SelectedCharacter.Description = DescriptionText;
                SelectedCharacter.Role = RoleText;
                SelectedCharacter.Age = AgeText;
                SelectedCharacter.TraitsText = TraitsText;
                SelectedCharacter.Appearance = AppearanceText;
                if (_pictureData is not null)
                    SelectedCharacter.Picture = _pictureData;
            }
        }

        [RelayCommand]
        public void CancelEdit()
        {
            if (_characterBeforeChange is not null)
            {
                NameText = _characterBeforeChange.Name;
                DescriptionText = _characterBeforeChange.Description;
                RoleText = _characterBeforeChange.Role ?? string.Empty;
                AgeText = _characterBeforeChange.Age ?? string.Empty;
                TraitsText = _characterBeforeChange.TraitsText ?? string.Empty;
                AppearanceText = _characterBeforeChange.Appearance ?? string.Empty;
                ProfilePicture = _characterBeforeChange.Picture?.Image;
                _pictureData = _characterBeforeChange.Picture;
            }

            ExitEditMode();
        }

        [RelayCommand]
        private void AddCharacter()
        {
            var ch = _projectState.CreateNewCharacter(
                ResourceLoader.GetForViewIndependentUse().GetString("newCharacterName"),
                string.Empty);
        }

        [RelayCommand]
        private void RemoveCharacter()
        {
            if (SelectedCharacter is not null)
            {
                _projectState.RemoveCharacter(SelectedCharacter.Token);
            }
        }

        [RelayCommand]
        private void Undo()
        {
            TimeTravelCharacter.Undo();
        }

        [RelayCommand]
        private void Redo()
        {
            TimeTravelCharacter.Redo();
        }

        public bool DidSomethingChange()
        {
            if (SelectedCharacter is null) return false;
            return SelectedCharacter.Name != NameText
                || SelectedCharacter.Description != DescriptionText
                || SelectedCharacter.Role != (string.IsNullOrEmpty(RoleText) ? null : RoleText)
                || SelectedCharacter.Age != (string.IsNullOrEmpty(AgeText) ? null : AgeText)
                || SelectedCharacter.TraitsText != TraitsText
                || SelectedCharacter.Appearance != AppearanceText
                || SelectedCharacter.Picture?.Image != ProfilePicture;
        }

        private void SortCharacters()
        {
            _projectState.SortCharacters();
            OnPropertyChanged(nameof(Characters));
        }

        public void SetPicture(CharacterPicture picture, BitmapImage image)
        {
            _pictureData = picture;
            ProfilePicture = image;
        }
    }
}
