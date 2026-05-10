using Storylines.Views.Controls;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.ViewModels;
using System;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Storylines.Models;
using Storylines.Services.Modes;

namespace Storylines.Views.Pages
{
    public sealed partial class MainPage : Page
    {
        private static IProjectPersistenceService Persistence => App.GetService<IProjectPersistenceService>();

        private ChaptersList ChapterList => _windowContext.ChapterList;
        private MainCommandBar CommandBar => _windowContext.CommandBar;
        private Storylines.Views.Controls.DialogueEditor.BranchingDialogueEditor ChapterText => _windowContext.ChapterText;

        private readonly EventAggregator _events;
        private readonly MainPageViewModel _viewModel;
        private readonly EditorModeService _modeService;
        private readonly WindowContext _windowContext;
        private string _pendingChapterToken;

        private DispatcherTimer _sessionTimer;
        private bool _textFormattingContextActive;

        public MainPageViewModel ViewModel => _viewModel;
        public bool IsEditorCommandContextActive => _textFormattingContextActive && ViewModel.IsChapterSelected && !ViewModel.IsChapterTextReadOnly;

        public MainPage()
        {
            _windowContext = App.GetService<WindowContext>();
            _events = App.GetService<EventAggregator>();
            _viewModel = App.GetService<MainPageViewModel>();
            _modeService = App.GetService<EditorModeService>();

            InitializeComponent();

            _windowContext.MainPage = this;
            App.GetService<IWindowManager>().SetCurrent(_windowContext);

            _windowContext.AppView.page = AppView.Pages.MainPage;

            _events.Subscribe<ChapterToolsStateEvent>(e => OnChapterToolsStateChanged(e.Enabled));
            _events.Subscribe<ToggleChapterListEvent>(e => OpenOrCloseChapterList(e.Open, e.Manually));
            _events.Subscribe<RefreshNotesPaneEvent>(_ => RefreshNotesPane());
            _events.Subscribe<SettingChangedEvent>(OnSettingChanged);
            _modeService.ModeChanged += ApplyModeSurfaceLayout;

            SizeChanged();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _pendingChapterToken = e.Parameter as string;
            TrySelectPendingChapter();
        }

        private void OnSettingChanged(SettingChangedEvent e)
        {
            if (e.SettingKey == SettingsValueStrings.ZoomValue && ChapterText is not null)
                SetZoomValue(Convert.ToInt32(e.Value));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var pendingActivatedItem = _windowContext.PendingActivatedItem ?? App.PendingActivatedItem;
            if (pendingActivatedItem is not null)
            {
                Persistence.DefaultLaunch(pendingActivatedItem);
                _windowContext.PendingActivatedItem = null;
                if (ReferenceEquals(App.PendingActivatedItem, pendingActivatedItem))
                    App.PendingActivatedItem = null;
            }

            var selectedChapterIndex = ChapterList.ViewModel.SelectedIndex;
            if (ChapterList.listView.Items.Count > 0
                && selectedChapterIndex >= 0
                && selectedChapterIndex < ChapterList.listView.Items.Count)
            {
                ChapterList.listView.SelectedIndex = selectedChapterIndex;
            }

            TrySelectPendingChapter();

            ChapterText.TextBoxWhiteBackground(App.GetService<IPreferencesService>().Get(SettingsValueStrings.TextBoxSolidBackground, false));

            LoadTextBoxZoom();
            ApplyModeSurfaceLayout(_modeService.Current);
            RefreshFormattingCommandAvailability();

            if (Persistence.CurrentProject?.file is not null)
                EnableOrDisableToolsForStorylinesDocuments(Persistence.CurrentProject.file.FileType.Contains(".srl"));
        }

        /// <summary>
        /// Handles the UI-level side of chapter selection state changes.
        /// ViewModel handles text/visibility via OnIsChapterSelectedChanged.
        /// </summary>
        private void OnChapterToolsStateChanged(bool enabled)
        {
            ViewModel.IsChapterSelected = enabled;

            // Child control reach-through
            ChapterText.textBox.IsTabStop = enabled;
            ChapterText.textBoxRectangle.IsHitTestVisible = !enabled;
            ChapterText.textBox.IsHitTestVisible = enabled;

            RefreshFormattingCommandAvailability();
            CommandBar.searchReplaceButton.IsEnabled = enabled;

            if (enabled)
            {
                ChapterText.textBoxRectangle.Visibility = Visibility.Collapsed;
            }
            else
            {
                _windowContext.AppView.Focus(FocusState.Keyboard);
                ChapterText.textBoxRectangle.Visibility = Visibility.Visible;
            }
        }

        public void EnableOrDisableToolsForStorylinesDocuments(bool enable)
        {
            ChapterList.canAdd = enable;
            ChapterList.listView.IsEnabled = enable;

            CommandBar.exportButton.IsEnabled = enable;
            CommandBar.charactersButton.IsEnabled = enable;
        }

        public void SetTextFormattingContextActive(bool active)
        {
            _textFormattingContextActive = active;
            RefreshFormattingCommandAvailability();
            CommandBar?.RefreshSpeechCommandAvailability();
        }

        private void TrySelectPendingChapter()
        {
            if (string.IsNullOrWhiteSpace(_pendingChapterToken) || ChapterList?.ViewModel is null)
                return;

            var projectState = App.TryGetService<ProjectState>();
            var chapter = projectState?.FindChapter(_pendingChapterToken);
            if (chapter is null)
                return;

            var chapterIndex = projectState.FindChapterID(_pendingChapterToken);
            ChapterList.ViewModel.SelectedIndex = chapterIndex;

            if (ChapterList.listView is not null
                && chapterIndex >= 0
                && chapterIndex < ChapterList.listView.Items.Count)
            {
                ChapterList.listView.SelectedIndex = chapterIndex;
            }

            _pendingChapterToken = null;
        }

        public void RefreshFormattingCommandAvailability()
        {
            if (CommandBar is null)
                return;

            var enableFormatting = ViewModel.IsChapterSelected
                && _textFormattingContextActive
                && !ViewModel.IsChapterTextReadOnly;

            CommandBar.SetFormattingCommandsEnabled(enableFormatting);

            if (!enableFormatting)
                CommandBar.ClearFormattingCommandState();
        }

        private void OnPage_SizeChanged(object sender, SizeChangedEventArgs e) => SizeChanged();

        public new void SizeChanged()
        {
            ApplyModeSurfaceLayout(_modeService.Current);
        }

        private void ApplyModeSurfaceLayout(IEditorMode mode)
        {
            ApplyChapterListLayout(mode);
            ApplyEditorMargin(mode);
        }

        private void ApplyChapterListLayout(IEditorMode mode)
        {
            bool showChapterList = mode?.Chrome.ShowChapterList ?? true;
            closeOpenChapterListComponent.Visibility = showChapterList ? Visibility.Visible : Visibility.Collapsed;

            if (!showChapterList)
            {
                SetChapterListLayout(false);
                return;
            }

            OpenOrCloseChapterList(ActualWidth >= 800, false);
        }

        private void ApplyEditorMargin(IEditorMode mode)
        {
            double edgeInset = ActualWidth >= 800 ? 20 : 8;
            double topInset = edgeInset;

            if (mode?.Chrome.OverlayContent is not null)
                topInset += mode.Id == "focus" ? 68 : 12;

            chapterTextBoxMainPage.Margin = new Thickness(edgeInset, topInset, edgeInset, edgeInset);
        }

        private void SetChapterListLayout(bool open)
        {
            if (!open)
            {
                chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 2);
                mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                mainGrid.ColumnDefinitions[1].MinWidth = 0;
                closeOpenChapterListComponentIcon.Symbol = Symbol.ClosePane;
            }
            else
            {
                chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 1);
                mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                mainGrid.ColumnDefinitions[1].MinWidth = 220;
                closeOpenChapterListComponentIcon.Symbol = Symbol.OpenPane;
            }

            UpdateTextBoxZoom(textBoxZoomSlider.Value);
        }

        public void OpenOrCloseChapterList(bool open, bool manually)
        {
            if (!open)
            {
                SetChapterListLayout(false);

                if (!ChapterList.closedManually)
                    ChapterList.closedManually = manually;
            }
            else if (!ChapterList.closedManually || manually)
            {
                SetChapterListLayout(true);

                ChapterList.closedManually = false;
            }
        }

        #region DownBar
        public void UpdateDownBar() => ViewModel.UpdateDownBar();

        private void OnCloseChapterListComponent_Click(object sender, RoutedEventArgs e) =>
            OpenOrCloseChapterList(closeOpenChapterListComponentIcon.Symbol == Symbol.ClosePane, true);
        #endregion

        #region Session Timer
        public void StartSessionTimer()
        {
            int wordCount = RtfHelper.GetTotalWordCount(App.GetService<ProjectState>().Chapters);
            if (!ViewModel.StartSession(wordCount)) return;

            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sessionTimer.Tick += (s, _) => ViewModel.OnSessionTimerTick();
            _sessionTimer.Start();
        }

        #endregion

        #region Notes
        public void ToggleNotesPane(bool show)
        {
            ViewModel.NotesPaneVisible = show;

            if (show)
            {
                notesRow.Height = new GridLength(220);
                chapterNotesPane.Visibility = Visibility.Visible;
                chapterNotesPane.LoadNotes();
            }
            else
            {
                notesRow.Height = new GridLength(0);
                chapterNotesPane.Visibility = Visibility.Collapsed;
            }

            if (CommandBar?.notesToggleButton is not null && CommandBar.notesToggleButton.IsChecked != show)
                CommandBar.notesToggleButton.IsChecked = show;
        }

        public void RefreshNotesPane()
        {
            if (chapterNotesPane.Visibility == Visibility.Visible)
                chapterNotesPane.LoadNotes();
        }

        public void ShowWelcomePanel(bool show) => ViewModel.ShowWelcomePanel(show);
        #endregion

        #region Zoom
        public void SetZoomValue(int value)
        {
            if (value >= 13 && value <= 100)
                textBoxZoomSlider.Value = value;
        }

        private void OnTextBoxZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ChapterList.listView.SelectedItem is not null)
            {
                UpdateTextBoxZoom(textBoxZoomSlider.Value);
                App.GetService<IPreferencesService>().Set(SettingsValueStrings.ZoomValue, textBoxZoomSlider.Value);
            }
        }

        public void UpdateTextBoxZoom(double sliderValue)
        {
            double scale = sliderValue / 25;
            _ = ChapterText.textBoxScrollViewer.ChangeView(null, null, (float)scale);

            double viewportWidth = ChapterText.textBoxScrollViewer.ActualWidth;
            if (viewportWidth > 0 && scale > 0)
            {
                double desiredWidth = viewportWidth * (1 / scale);
                ChapterText.textBox.Width = Math.Max(desiredWidth, viewportWidth);
            }

            ViewModel.ZoomLevel = sliderValue;
        }

        public void LoadTextBoxZoom()
        {
            textBoxZoomSlider.Value = App.GetService<IPreferencesService>().Get<double>(SettingsValueStrings.ZoomValue, 25);
            UpdateTextBoxZoom(textBoxZoomSlider.Value);
        }

        private void OnTextBoxZoomText_Click(object sender, RoutedEventArgs e)
        {
            textBoxZoomTextFlyout.ShowAt(textBoxZoomText);
            textBoxZoomTextFlyoutTextBox.Value = textBoxZoomSlider.Value * 4;
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            textBoxZoomSlider.Value = 25;
            textBoxZoomTextFlyoutTextBox.Value = 100;
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
