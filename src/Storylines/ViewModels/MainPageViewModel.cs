using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Services.Modes;
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

        // ── Mode-driven shell bindings (driven by EditorModeService.Current.Chrome) ──
        [ObservableProperty]
        private Visibility _defaultCommandBarVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility _modeChapterListVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility _formattingBarVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility _downBarStatsVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility _downBarFocusVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private bool _isChapterTextReadOnly;

        [ObservableProperty]
        private object _modeOverlayContent;

        [ObservableProperty]
        private string _currentModeId = "edit";

        /// <summary>Tracks whether we already showed the "goal reached" notification for the current chapter session.</summary>
        public bool WordGoalCelebrated { get; set; }

        public ObservableCollection<Chapter> Chapters => _projectState.Chapters;

        public MainPageViewModel(
            ProjectState projectState = null,
            IDialogService dialogs = null,
            EventAggregator events = null,
            EditorModeService modeService = null)
        {
            _projectState = projectState ?? App.TryGetService<ProjectState>() ?? new ProjectState();
            _dialogs = dialogs ?? App.TryGetService<IDialogService>() ?? new DialogService();
            events ??= App.TryGetService<EventAggregator>() ?? new EventAggregator();
            DownBarText = ResourceLoader.GetForViewIndependentUse().GetString("downBarTextS");

            events.Subscribe<ToolsStateChangedEvent>(e =>
            {
                IsStorylinesDocument = e.IsStorylinesDocument;
            });

            modeService ??= App.TryGetService<EditorModeService>();
            if (modeService != null)
            {
                ApplyModeChrome(modeService.Current);
                modeService.ModeChanged += ApplyModeChrome;
            }
        }

        private void ApplyModeChrome(IEditorMode mode)
        {
            var chrome = mode.Chrome;
            DefaultCommandBarVisibility = chrome.ShowDefaultCommandBar ? Visibility.Visible : Visibility.Collapsed;
            ModeChapterListVisibility = chrome.ShowChapterList ? Visibility.Visible : Visibility.Collapsed;
            FormattingBarVisibility = chrome.ShowChapterTextFormattingBar ? Visibility.Visible : Visibility.Collapsed;
            DownBarStatsVisibility = chrome.ShowDownBarStats ? Visibility.Visible : Visibility.Collapsed;
            DownBarFocusVisibility = chrome.ShowDownBarFocusText ? Visibility.Visible : Visibility.Collapsed;
            IsChapterTextReadOnly = chrome.IsTextReadOnly;
            ModeOverlayContent = chrome.OverlayContent;
            CurrentModeId = mode.Id;
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
            WelcomePanelVisibility = Visibility.Collapsed;
            ChapterTextVisibility = Visibility.Visible;
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
