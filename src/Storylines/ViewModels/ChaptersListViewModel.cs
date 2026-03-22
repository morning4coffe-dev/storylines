using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Scripts.Services;
using Storylines.Scripts.Services.Interfaces;
using Storylines.Scripts.Variables;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;

namespace Storylines.ViewModels
{
    public partial class ChaptersListViewModel : ObservableObject
    {
        private readonly ProjectState _projectState;
        private readonly IDialogService _dialogs;

        public ObservableCollection<Chapter> Chapters => _projectState.Chapters;

        [ObservableProperty]
        private Chapter _selectedChapter;

        [ObservableProperty]
        private int _selectedIndex = -1;

        [ObservableProperty]
        private bool _canAdd = true;

        [ObservableProperty]
        private Visibility _noChaptersPlaceholderVisibility = Visibility.Visible;

        [ObservableProperty]
        private bool _isExportEnabled;

        [ObservableProperty]
        private bool _isSaveEnabled;

        [ObservableProperty]
        private bool _isSaveCopyEnabled;

        [ObservableProperty]
        private bool _isAddButtonEnabled = true;

        public bool SwitchedChapters { get; set; }
        public bool ClosedManually { get; set; }

        public ChaptersListViewModel()
        {
            _projectState = ServiceLocator.ProjectState;
            _dialogs = ServiceLocator.Dialogs;
            UpdateListState();
        }

        partial void OnSelectedChapterChanged(Chapter value)
        {
            UpdateListState();
        }

        partial void OnCanAddChanged(bool value)
        {
            IsAddButtonEnabled = value;
            UpdateListState();
        }

        public void UpdateListState()
        {
            NoChaptersPlaceholderVisibility = _projectState.Chapters.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            IsExportEnabled = _projectState.Chapters.Count > 0 || _projectState.Characters.Count > 0;
            IsSaveEnabled = _projectState.Chapters.Count > 0;
            IsSaveCopyEnabled = _projectState.Chapters.Count > 0;
            IsAddButtonEnabled = CanAdd;
        }

        [RelayCommand]
        private void AddChapter()
        {
            _dialogs.OpenChapterCreator();
        }

        [RelayCommand]
        private void RenameChapter(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                var chapter = _projectState.FindChapter(token);
                if (chapter != null)
                    _dialogs.OpenChapterRenamer(chapter);
            }
        }

        [RelayCommand]
        private void RemoveChapter(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _projectState.RemoveChapter(token);
                UpdateListState();
            }
        }
    }
}
