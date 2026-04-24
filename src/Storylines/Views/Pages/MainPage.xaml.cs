using Storylines.Views.Dialogs;
using Storylines.Views.Controls;
using Storylines.Helpers;
using Storylines.Helpers.Modes;
using Storylines.Services;
using Storylines.ViewModels;
using System;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Storylines.Views.Pages
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Current { get; private set; }

        public static ChaptersList ChapterList;
        public static MainCommandBar CommandBar;
        public static ChapterTextBox ChapterText;

        public static FocusMode FocusMode;
        public static ReadMode ReadMode;

        public MainPageViewModel ViewModel => ServiceLocator.MainPageViewModel;

        // Session timer
        private DispatcherTimer _sessionTimer;
        private DateTimeOffset _sessionStart;
        private bool _sessionActive;

        public MainPage()
        {
            InitializeComponent();
            Current = this;

            AppView.current.page = AppView.Pages.MainPage;

            ServiceLocator.Events.Subscribe<ChapterToolsStateEvent>(e => EnableOrDisableChapterTools(e.Enabled));
            ServiceLocator.Events.Subscribe<ToggleChapterListEvent>(e => OpenOrCloseChapterList(e.Open, e.Manually));
            ServiceLocator.Events.Subscribe<RefreshNotesPaneEvent>(_ => RefreshNotesPane());
            ServiceLocator.Events.Subscribe<SessionStatsUpdatedEvent>(OnSessionStatsUpdated);

            SizeChanged();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.item != null)
            {
                SaveSystem.DefaultLaunch(App.item);
                App.item = null;
            }

            if (ChapterList.listView.Items.Count > 0 && ChaptersList.selectedIndex <= ChapterList.listView.Items.Count)
                ChapterList.listView.SelectedIndex = ChaptersList.selectedIndex;
            ChapterText.TextBoxWhiteBackground(Convert.ToBoolean(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.TextBoxSolidBackground] ?? false));

            LoadTextBoxZoom();

            if (SaveSystem.currentProject != null && SaveSystem.currentProject.file != null)
                EnableOrDisableToolsForStorylinesDocuments(SaveSystem.currentProject.file.FileType.Contains(".srl"));
        }

        public void EnableOrDisableChapterTools(bool enable)
        {
            ViewModel.IsChapterSelected = enable;

            ChapterText.textBox.IsTabStop = enable;
            ChapterText.textBoxRectangle.IsHitTestVisible = !enable;
            ChapterText.textBox.IsHitTestVisible = enable;
            textBoxZoomSlider.IsEnabled = enable;
            textBoxZoomTextHyperlink.IsEnabled = enable;

            if (enable)
            {
                ChapterText.textBoxRectangle.Visibility = Visibility.Collapsed;
                UpdateDownBar();
            }
            else
            {
                AppView.current.Focus(FocusState.Keyboard);
                ChapterText.textBoxRectangle.Visibility = Visibility.Visible;
                ViewModel.DownBarText = ResourceLoader.GetForCurrentView().GetString("downBarTextS");
            }
        }

        public void EnableOrDisableToolsForStorylinesDocuments(bool enable)
        {
            ChapterList.canAdd = enable;
            ChapterList.listView.IsEnabled = enable;

            CommandBar.exportButton.IsEnabled = enable;
            CommandBar.charactersButton.IsEnabled = enable;

            ChapterText.chapterTextCommandBar.IsEnabled = enable;
        }

        private void OnPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChanged();
        }

        public new void SizeChanged()
        {
            if (ActualWidth < 800)
            {
                OpenOrCloseChapterList(false, false);
            }
            else
            {
                OpenOrCloseChapterList(true, false);
            }

            UpdateTextBoxZoom(textBoxZoomSlider.Value);
        }

        public void OpenOrCloseChapterList(bool open, bool manually)
        {
            double addOrSubtract = 0;

            if (!open)
            {
                chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 2);
                mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                mainGrid.ColumnDefinitions[1].MinWidth = 0;
                closeOpenChapterListComponentIcon.Symbol = Symbol.ClosePane;
                addOrSubtract = chapterListComponentMainPage.ActualWidth;

                if (!ChapterList.closedManually)
                    ChapterList.closedManually = manually;
            }
            else
            {
                if (!ChapterList.closedManually || manually)
                {
                    chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 1);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[1].MinWidth = 220;
                    closeOpenChapterListComponentIcon.Symbol = Symbol.OpenPane;
                    addOrSubtract = -chapterListComponentMainPage.ActualWidth;

                    ChapterList.closedManually = false;
                }
            }

            ChapterText.textBox.Width = (ChapterText.textBoxScrollViewer.ActualWidth + addOrSubtract) * (1 / (textBoxZoomSlider.Value / 25));
        }

        #region DownBar
        public void UpdateDownBar() => ProjectStatsDialogue.UpdateDownBar();

        private void OnDownBarText_Click(object sender, RoutedEventArgs e) => ProjectStatsDialogue.Open(true);

        private void OnCloseChapterListComponent_Click(object sender, RoutedEventArgs e) =>
            OpenOrCloseChapterList(closeOpenChapterListComponentIcon.Symbol == Symbol.ClosePane, true);
        #endregion

        #region Session Timer / Writing Streak
        public void StartSessionTimer()
        {
            if (_sessionActive) return;

            _sessionActive = true;
            _sessionStart = DateTimeOffset.Now;
            sessionStreakButton.Visibility = Visibility.Visible;

            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sessionTimer.Tick += OnSessionTimer_Tick;
            _sessionTimer.Start();

            // Seed the word baseline
            int wordCount = GetTotalProjectWordCount();
            WritingSessionService.OnSessionStart(wordCount);
            UpdateStreakBadge();
        }

        private void OnSessionTimer_Tick(object sender, object e)
        {
            var elapsed = DateTimeOffset.Now - _sessionStart;
            sessionTimerText.Text = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        }

        private void OnSessionStatsUpdated(SessionStatsUpdatedEvent e)
        {
            UpdateStreakBadge();
            UpdateWordGoalBar();
        }

        private void UpdateStreakBadge()
        {
            int streak = WritingSessionService.GetCurrentStreak();
            int today = WritingSessionService.GetTodayWords();
            streakText.Text = streak > 0 ? $"🔥 {streak}d · {today}w today" : $"{today}w today";
        }

        private void OnSessionStreak_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowProjectStats();
        }

        public void UpdateWordGoalBar()
        {
            var selectedIndex = ServiceLocator.TextEditor.SelectedChapterIndex;
            if (selectedIndex < 0 || selectedIndex >= ServiceLocator.ProjectState.Chapters.Count)
            {
                wordGoalProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            var chapter = ServiceLocator.ProjectState.Chapters[selectedIndex];
            if (chapter.WordCountGoal == null || chapter.WordCountGoal <= 0)
            {
                wordGoalProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            ChapterText.textBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string text);
            int wordCount = text.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            double progress = Math.Min(100.0, wordCount * 100.0 / chapter.WordCountGoal.Value);

            wordGoalProgressBar.Value = progress;
            wordGoalProgressBar.Visibility = Visibility.Visible;

            // Celebrate hitting the goal
            if (progress >= 100 && !ViewModel.WordGoalCelebrated)
            {
                ViewModel.WordGoalCelebrated = true;
                NotificationManager.DisplayInAppNotification(
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success,
                    "Chapter goal reached!",
                    $"You've hit your word goal for \"{chapter.Name}\". Keep writing! 🎉");
            }
            else if (progress < 100)
            {
                ViewModel.WordGoalCelebrated = false;
            }
        }

        private int GetTotalProjectWordCount()
        {
            string all = string.Empty;
            foreach (var chapter in ServiceLocator.ProjectState.Chapters)
            {
                if (!string.IsNullOrEmpty(chapter.Text))
                {
                    var box = new RichEditBox();
                    box.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, chapter.Text);
                    box.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string txt);
                    all += txt;
                }
            }
            return all.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
        #endregion

        #region Notes
        public void ToggleNotesPane(bool show)
        {
            if (show)
            {
                notesRow.Height = new GridLength(140);
                chapterNotesPane.Visibility = Visibility.Visible;
                chapterNotesPane.LoadNotes();
            }
            else
            {
                notesRow.Height = new GridLength(0);
                chapterNotesPane.Visibility = Visibility.Collapsed;
            }
        }

        public void RefreshNotesPane()
        {
            if (chapterNotesPane.Visibility == Visibility.Visible)
                chapterNotesPane.LoadNotes();
        }

        public void ShowWelcomePanel(bool show)
        {
            ViewModel.ShowWelcomePanel(show);
        }
        #endregion

        #region Zoom
        private void OnTextBoxZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ChapterList.listView.SelectedItem != null)
            {
                UpdateTextBoxZoom(textBoxZoomSlider.Value);
                ApplicationData.Current.LocalSettings.Values["TextBoxZoomValue"] = textBoxZoomSlider.Value;
            }
        }

        public void UpdateTextBoxZoom(double sliderValue)
        {
            double sliderOne = sliderValue / 25;
            _ = ChapterText.textBoxScrollViewer.ChangeView(null, null, (float)sliderOne);

            ChapterText.textBox.Width = ChapterText.textBoxScrollViewer.ActualWidth * (1 / sliderOne);
            textBoxZoomText.Text = $"{Math.Round(sliderOne * 100)}%";
        }

        public void LoadTextBoxZoom()
        {
            textBoxZoomSlider.Value = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values["TextBoxZoomValue"] ?? 25);
            Current.UpdateTextBoxZoom(textBoxZoomSlider.Value);
        }

        private void OnTextBoxZoomText_Click(object sender, RoutedEventArgs e)
        {
            textBoxZoomTextFlyout.ShowAt(textBoxZoomText);
            textBoxZoomTextFlyoutTextBox.Value = textBoxZoomSlider.Value * 4;
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            textBoxZoomSlider.Value = 25;
            textBoxZoomTextFlyoutTextBox.Value = textBoxZoomSlider.Value * 4;
            textBoxZoomTextFlyoutTextBox.Text = "100%";
        }

        private void OnTextBoxZoomTextFlyoutTextBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!double.IsNaN(sender.Value))
                textBoxZoomSlider.Value = sender.Value / 4;
        }
        #endregion
    }
}
