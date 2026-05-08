using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Constants;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Services.Modes;
using Storylines.Models;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;

namespace Storylines.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private static readonly TimeSpan WordGoalNotificationDuration = TimeSpan.FromSeconds(LayoutConstants.NotificationDismissSeconds - 4);

        private readonly ProjectState _projectState;
        private readonly IDialogService _dialogs;
        private readonly ITextEditorService _textEditor;
        private readonly ResourceLoader _resources;
        private readonly INotificationService _notifications;
        private readonly IWritingSessionService _writingSession;

        // ── Chapter state ──

        [ObservableProperty]
        private bool _isChapterSelected;

        [ObservableProperty]
        private bool _isChapterListOpen = true;

        // ── Zoom ──

        [ObservableProperty]
        private double _zoomLevel = 25;

        [ObservableProperty]
        private string _zoomText = "100%";

        // ── Down bar stats ──

        [ObservableProperty]
        private string _downBarText = string.Empty;

        [ObservableProperty]
        private string _downBarWordsText = string.Empty;

        [ObservableProperty]
        private string _downBarCharsText = string.Empty;

        [ObservableProperty]
        private string _downBarReadTimeText = string.Empty;

        [ObservableProperty]
        private string _downBarChapterName = string.Empty;

        [ObservableProperty]
        private Visibility _downBarSeparatorsVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _downBarGuidanceText = string.Empty;

        [ObservableProperty]
        private Visibility _downBarGuidanceVisibility = Visibility.Visible;

        // ── Session / Streak ──

        [ObservableProperty]
        private string _sessionTimerText = string.Empty;

        [ObservableProperty]
        private string _streakText = string.Empty;

        [ObservableProperty]
        private Visibility _sessionStreakVisibility = Visibility.Collapsed;

        private DateTimeOffset _sessionStart;
        private bool _sessionActive;

        // ── Word goal ──

        [ObservableProperty]
        private double _wordGoalProgress;

        [ObservableProperty]
        private Visibility _wordGoalVisibility = Visibility.Collapsed;

        /// <summary>Tracks whether we already showed the "goal reached" notification for the current chapter session.</summary>
        public bool WordGoalCelebrated { get; set; }

        // ── Panel visibility ──

        [ObservableProperty]
        private Visibility _welcomePanelVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _chapterTextVisibility = Visibility.Visible;

        [ObservableProperty]
        private bool _notesPaneVisible;

        [ObservableProperty]
        private bool _isStorylinesDocument = true;

        // ── Mode chrome ──

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
        private string _downBarFocusText = string.Empty;

        [ObservableProperty]
        private bool _isChapterTextReadOnly;

        [ObservableProperty]
        private object _modeOverlayContent;

        [ObservableProperty]
        private string _currentModeId = "edit";

        public ObservableCollection<Chapter> Chapters => _projectState.Chapters;

        public MainPageViewModel(
            ProjectState projectState,
            IDialogService dialogs,
            EventAggregator events,
            EditorModeService modeService,
            ITextEditorService textEditor,
            INotificationService notifications,
            IWritingSessionService writingSession)
        {
            _projectState = projectState;
            _dialogs = dialogs;
            _textEditor = textEditor;
            _notifications = notifications;
            _writingSession = writingSession;
            _resources = ResourceLoader.GetForViewIndependentUse();

            DownBarGuidanceText = _resources.GetString("downBarTextS");
            DownBarText = _resources.GetString("downBarTextS");

            events.Subscribe<ToolsStateChangedEvent>(e => IsStorylinesDocument = e.IsStorylinesDocument);
            events.Subscribe<FocusModeDownBarTextChangedEvent>(e => DownBarFocusText = e.Text);
            events.Subscribe<SessionStatsUpdatedEvent>(_ =>
            {
                UpdateStreakBadge();
                UpdateWordGoalBar();
            });

            ApplyModeChrome(modeService.Current);
            modeService.ModeChanged += ApplyModeChrome;
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

        // ── Chapter selection ──

        partial void OnIsChapterSelectedChanged(bool value)
        {
            if (value)
            {
                DownBarGuidanceVisibility = Visibility.Collapsed;
                DownBarSeparatorsVisibility = Visibility.Visible;
                UpdateDownBar();
                ShowWelcomePanel(false);
            }
            else
            {
                DownBarText = _resources.GetString("downBarTextS");
                DownBarWordsText = string.Empty;
                DownBarCharsText = string.Empty;
                DownBarReadTimeText = string.Empty;
                DownBarChapterName = string.Empty;
                DownBarSeparatorsVisibility = Visibility.Collapsed;
                DownBarGuidanceText = _resources.GetString("downBarTextS");
                DownBarGuidanceVisibility = Visibility.Visible;
                ShowWelcomePanel(_projectState.Chapters.Count == 0);
            }
        }

        // ── Zoom ──

        partial void OnZoomLevelChanged(double value)
        {
            double scale = value / 25;
            ZoomText = $"{Math.Round(scale * 100)}%";
        }

        // ── Down bar ──

        public void UpdateDownBar()
        {
            if (_textEditor is null) return;

            var selectedIndex = _textEditor.SelectedChapterIndex;
            if (selectedIndex < 0 || selectedIndex >= _projectState.Chapters.Count)
            {
                DownBarWordsText = string.Empty;
                DownBarCharsText = string.Empty;
                DownBarReadTimeText = string.Empty;
                DownBarChapterName = string.Empty;
                DownBarSeparatorsVisibility = Visibility.Collapsed;
                DownBarText = _resources.GetString("downBarTextS");
                return;
            }

            string text = _textEditor.GetText(TextFormat.PlainText);
            int charCount = text.Length > 0 ? text.Length - 1 : 0;
            int wordCount = text.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;

            int selectedLen = _textEditor.SelectedTextLength;
            string selectedPrefix = selectedLen > 0 ? $"{selectedLen} / " : "";

            int readMinutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));

            DownBarWordsText = $"{_resources.GetString("words")}: {wordCount}";
            DownBarCharsText = $"{_resources.GetString("charactersStory")}: {selectedPrefix}{charCount}";
            DownBarReadTimeText = $"~{readMinutes} {_resources.GetString("readTimeMinRead")}";

            DownBarChapterName = _projectState.Chapters[selectedIndex].Name;

            int paragraphCount = Regex.Matches(text,
                @"[^\r\n]*[^ \r\n]+[^\r\n]*((\r|\n|\r\n)[^\r\n]*[^ \r\n]+[^\r\n]*)*").Count;
            DownBarText = $"{_resources.GetString("charactersStory")}: {selectedPrefix}{charCount}   {_resources.GetString("words")}: {wordCount}   {_resources.GetString("paragraphs")}: {paragraphCount}";
        }

        // ── Word goal ──

        public void UpdateWordGoalBar()
        {
            if (_textEditor is null) return;

            var selectedIndex = _textEditor.SelectedChapterIndex;
            if (selectedIndex < 0 || selectedIndex >= _projectState.Chapters.Count)
            {
                WordGoalVisibility = Visibility.Collapsed;
                return;
            }

            var chapter = _projectState.Chapters[selectedIndex];
            if (chapter.WordCountGoal is null || chapter.WordCountGoal <= 0)
            {
                WordGoalVisibility = Visibility.Collapsed;
                return;
            }

            string text = _textEditor.GetText(TextFormat.PlainText);
            int wordCount = text.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            double progress = Math.Min(100.0, wordCount * 100.0 / chapter.WordCountGoal.Value);

            WordGoalProgress = progress;
            WordGoalVisibility = Visibility.Visible;

            if (progress >= 100 && !WordGoalCelebrated)
            {
                WordGoalCelebrated = true;
                _notifications.ShowNotification(new NotificationRequest
                {
                    Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success,
                    Title = _resources.GetString("wordGoalReachedTitle"),
                    Message = string.Format(_resources.GetString("wordGoalReachedMessage"), chapter.Name),
                    Duration = WordGoalNotificationDuration
                });
            }
            else if (progress < 100)
            {
                WordGoalCelebrated = false;
            }
        }

        // ── Session / Streak ──

        public void UpdateStreakBadge()
        {
            int streak = _writingSession.GetCurrentStreak();
            int today = _writingSession.GetTodayWords();
            StreakText = streak > 0 ? $"🔥 {streak}d · {today}w today" : $"{today}w today";
        }

        /// <summary>
        /// Begins tracking a writing session. Returns true if a new session was
        /// started (caller should create a DispatcherTimer).
        /// </summary>
        public bool StartSession(int totalProjectWordCount)
        {
            if (_sessionActive) return false;

            _sessionActive = true;
            _sessionStart = DateTimeOffset.Now;
            SessionStreakVisibility = Visibility.Visible;

            _writingSession.OnSessionStart(totalProjectWordCount);
            UpdateStreakBadge();
            return true;
        }

        public void OnSessionTimerTick()
        {
            var elapsed = DateTimeOffset.Now - _sessionStart;
            SessionTimerText = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        }

        // ── Panel visibility ──

        public void ShowWelcomePanel(bool show)
        {
            WelcomePanelVisibility = Visibility.Collapsed;
            ChapterTextVisibility = Visibility.Visible;
        }

        [RelayCommand]
        private void ToggleNotesPane() => NotesPaneVisible = !NotesPaneVisible;

        [RelayCommand]
        public void ShowProjectStats() => _dialogs.OpenProjectStats(true);

        [RelayCommand]
        private void ShowDetailedStats() => _dialogs.OpenProjectStats(false);
    }
}
