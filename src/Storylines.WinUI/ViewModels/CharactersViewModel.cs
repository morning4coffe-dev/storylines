using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.WinUI.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Storylines.WinUI.ViewModels
{
    public partial class CharactersViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Character> _characters;

        [ObservableProperty]
        private Character _selectedCharacter;

        [ObservableProperty]
        private bool _isEditModeEnabled;

        [ObservableProperty]
        private bool _isAddEnabled = true;

        [ObservableProperty]
        private bool _isRemoveEnabled = true;

        private Character _characterBeforeChange;

        public CharactersViewModel()
        {
            LoadCharacters();
        }

        private void LoadCharacters()
        {
            // TODO: Replace with actual data loading logic
            Characters = new ObservableCollection<Character>();
        }

        [RelayCommand]
        private void AddCharacter()
        {
            var newCharacter = new Character { Name = "New Character" };
            Characters.Add(newCharacter);
            SelectedCharacter = newCharacter;
            IsEditModeEnabled = true;
        }

        [RelayCommand]
        private void RemoveCharacter()
        {
            if (SelectedCharacter != null)
            {
                Characters.Remove(SelectedCharacter);
            }
        }

        [RelayCommand]
        private void EnableEditMode()
        {
            if (SelectedCharacter != null)
            {
                _characterBeforeChange = new Character
                {
                    Name = SelectedCharacter.Name,
                    Description = SelectedCharacter.Description,
                    Picture = SelectedCharacter.Picture
                };
                IsEditModeEnabled = true;
            }
        }

        [RelayCommand]
        private void ApplyChanges()
        {
            IsEditModeEnabled = false;
            SortCharacters();
        }

        [RelayCommand]
        private void CancelEdit()
        {
            if (SelectedCharacter != null && _characterBeforeChange != null)
            {
                SelectedCharacter.Name = _characterBeforeChange.Name;
                SelectedCharacter.Description = _characterBeforeChange.Description;
                SelectedCharacter.Picture = _characterBeforeChange.Picture;
            }
            IsEditModeEnabled = false;
        }

        private void SortCharacters()
        {
            Characters = new ObservableCollection<Character>(Characters.OrderBy(c => c.Name));
        }

        [RelayCommand]
        private async Task ChangeProfilePicture()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".gif");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
                StorageFile newFile = await file.CopyAsync(folder, file.Name, NameCollisionOption.ReplaceExisting);
                var image = new BitmapImage(new Uri(newFile.Path));

                if (SelectedCharacter != null)
                {
                    SelectedCharacter.Picture = new CharacterPicture { FileName = newFile.Name, Image = image };
                }
            }
        }
    }
}
