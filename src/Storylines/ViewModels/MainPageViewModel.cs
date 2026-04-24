using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;

namespace Storylines.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly ProjectState _projectState;
        private readonly IDialogService _dialogs;

        [ObservableProperty]
        private bool _isChapterSelected;

        [ObservableProperty]
        private bool _isChapterListOpen = true;

        [ObservableProperty]
        private double _zoomLevel = 25;

        [ObservableProperty]
        private string _zoomText = "100%";

        [ObservableProperty]
        private string _downBarText;

        [ObservableProperty]
        private Visibility _welcomePanelVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _chapterTextVisibility = Visibility.Visible;

        [ObservableProperty]
        private bool _notesPaneVisible;

        [ObservableProperty]
        private bool _isStorylinesDocument = true;

        /// <summary>Tracks whether we already showed the "goal reached" notification for the current chapter session.</summary>
        public bool WordGoalCelebrated { get; set; }

        public ObservableCollection<Chapter> Chapters => _projectState.Chapters;

        public MainPageViewModel()
        {
            _projectState = ServiceLocator.ProjectState;
            _dialogs = ServiceLocator.Dialogs;
            DownBarText = ResourceLoader.GetForViewIndependentUse().GetString("downBarTextS");

            ServiceLocator.Events.Subscribe<ToolsStateChangedEvent>(e =>
            {
                IsStorylinesDocument = e.IsStorylinesDocument;
            });
        }

        partial void OnIsChapterSelectedChanged(bool value)
        {
            if (!value)
            {
                DownBarText = ResourceLoader.GetForViewIndependentUse().GetString("downBarTextS");
                ShowWelcomePanel(_projectState.Chapters.Count == 0);
            }
            else
            {
                ShowWelcomePanel(false);
            }
        }

        partial void OnZoomLevelChanged(double value)
        {
            double sliderOne = value / 25;
            ZoomText = $"{System.Math.Round(sliderOne * 100)}%";
        }

        public void ShowWelcomePanel(bool show)
        {
            WelcomePanelVisibility = show ? Visibility.Visible : Visibility.Collapsed;
            ChapterTextVisibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        [RelayCommand]
        private void ToggleNotesPane()
        {
            NotesPaneVisible = !NotesPaneVisible;
        }

        [RelayCommand]
        public void ShowProjectStats()
        {
            _dialogs.OpenProjectStats(true);
        }

        [RelayCommand]
        private void ShowDetailedStats()
        {
            _dialogs.OpenProjectStats(false);
        }
    }
}
