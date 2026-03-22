using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Storylines.ViewModels
{
    public partial class CharactersPageViewModel : ObservableObject
    {
        private readonly ProjectState _projectState;

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
        private string _editButtonGlyph = "\uE70F";

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
        private bool _canUndo;

        [ObservableProperty]
        private bool _canRedo;

        public bool UnappliedChanges { get; set; }

        private Character _characterBeforeChange;

        public CharactersPageViewModel()
        {
            _projectState = ServiceLocator.ProjectState;
            EditButtonLabel = ResourceLoader.GetForCurrentView().GetString("editText");

            ServiceLocator.Events.Subscribe<UndoRedoStateChangedEvent>(OnUndoRedoStateChanged);
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
            if (value != null)
            {
                NameText = value.name ?? string.Empty;
                DescriptionText = value.description ?? string.Empty;
                RoleText = value.role ?? string.Empty;
                AgeText = value.age ?? string.Empty;
            }
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            if (IsEditMode)
            {
                // Leaving edit mode - apply changes if something changed
                if (SelectedCharacter != null && DidSomethingChange())
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

        private void EnterEditMode()
        {
            if (SelectedCharacter == null) return;

            IsEditMode = true;
            IsFieldsEnabled = true;
            IsListEnabled = false;

            _characterBeforeChange = _projectState.CopyCharacter(SelectedCharacter.token);

            CancelButtonVisibility = Visibility.Collapsed;
            EditButtonLabel = ResourceLoader.GetForCurrentView().GetString("cancelText");
            EditButtonGlyph = "\uE711";
            UnappliedChanges = false;
        }

        private void ExitEditMode()
        {
            IsEditMode = false;
            IsFieldsEnabled = false;
            IsListEnabled = true;

            CancelButtonVisibility = Visibility.Collapsed;
            EditButtonLabel = ResourceLoader.GetForCurrentView().GetString("editText");
            EditButtonGlyph = "\uE70F";
            UnappliedChanges = false;
        }

        public void ApplyChanges()
        {
            if (SelectedCharacter != null && _characterBeforeChange != null)
            {
                TimeTravelCharacter.SomethingChanged(TimeTravelCharacter.Changed.Changed, SelectedCharacter);
                UnappliedChanges = false;

                SelectedCharacter.name = NameText;
                SelectedCharacter.description = DescriptionText;
                SelectedCharacter.role = RoleText;
                SelectedCharacter.age = AgeText;
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            if (_characterBeforeChange != null)
            {
                NameText = _characterBeforeChange.name;
                DescriptionText = _characterBeforeChange.description;
                RoleText = _characterBeforeChange.role ?? string.Empty;
                AgeText = _characterBeforeChange.age ?? string.Empty;
            }

            ExitEditMode();
        }

        [RelayCommand]
        private void AddCharacter()
        {
            var ch = _projectState.CreateNewCharacter(
                ResourceLoader.GetForCurrentView().GetString("newCharacterName"),
                string.Empty);
        }

        [RelayCommand]
        private void RemoveCharacter()
        {
            if (SelectedCharacter != null)
            {
                _projectState.RemoveCharacter(SelectedCharacter.token);
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
            if (SelectedCharacter == null) return false;
            return SelectedCharacter.name != NameText
                || SelectedCharacter.description != DescriptionText
                || SelectedCharacter.role != (string.IsNullOrEmpty(RoleText) ? null : RoleText)
                || SelectedCharacter.age != (string.IsNullOrEmpty(AgeText) ? null : AgeText);
        }

        private void SortCharacters()
        {
            _projectState.SortCharacters();
            OnPropertyChanged(nameof(Characters));
        }
    }
}
