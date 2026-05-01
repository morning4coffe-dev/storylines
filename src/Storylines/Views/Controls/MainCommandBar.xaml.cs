using Storylines.Views.Pages;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Storylines.Views.Dialogs;

namespace Storylines.Views.Controls
{
    public sealed partial class MainCommandBar : UserControl
    {
        private readonly ChaptersListViewModel _chaptersListViewModel;
        private readonly IChapterWorkflowService _chapterWorkflow;
        private readonly INavigationService _navigation;
        private readonly ProjectState _projectState;
        private readonly ITextEditorService _textEditor;
        private readonly CommandBarViewModel _viewModel;
        private readonly SpeechHubViewModel _speechHub;
        private readonly ISpeechService _speechService;

        public CommandBarViewModel ViewModel => _viewModel;
        public SpeechHubViewModel SpeechHub => _speechHub;

        public MainCommandBar()
        {
            this.InitializeComponent();
            _chaptersListViewModel = App.GetService<ChaptersListViewModel>();
            _chapterWorkflow = App.GetService<IChapterWorkflowService>();
            _navigation = App.GetService<INavigationService>();
            _projectState = App.GetService<ProjectState>();
            _textEditor = App.GetService<ITextEditorService>();
            _viewModel = App.GetService<CommandBarViewModel>();
            _speechHub = App.GetService<SpeechHubViewModel>();
            _speechService = App.GetService<ISpeechService>();

            if(App.TryGetService<Storylines.Services.Modes.EditorModeService>()?.Current.Id == "edit"
               || App.TryGetService<Storylines.Services.Modes.EditorModeService>() == null)
                MainPage.CommandBar = this;

            UpdateExperimentalFeaturesVisibility();
            var events = App.GetService<EventAggregator>();
            events.Subscribe<SettingChangedEvent>(OnSettingChanged);
            events.Subscribe<TextFormattingStateChangedEvent>(OnTextFormattingStateChanged);

            // Restore persisted dialogue mode state
            dialoguesEnableButton.IsChecked = SettingsValues.dialogueModeEnabled;
        }

        private void OnSettingChanged(SettingChangedEvent e)
        {
            if (e.SettingKey == SettingsValueStrings.ExperimentalFeaturesEnabled)
                UpdateExperimentalFeaturesVisibility();
        }

        private void OnTextFormattingStateChanged(TextFormattingStateChangedEvent e)
        {
            mainBoldButton.IsChecked = e.IsBold;
            mainItalicButton.IsChecked = e.IsItalic;
            mainUnderlineButton.IsChecked = e.IsUnderlined;
            mainStrikethroughButton.IsChecked = e.IsStrikethrough;
        }

        private void UpdateExperimentalFeaturesVisibility()
        {
        }

        #region TEMP - NavigationView
        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (int.TryParse((sender.SelectedItem as Microsoft.UI.Xaml.Controls.NavigationViewItem).Tag.ToString(), out int i))
            {
                commandBarFile.Visibility = Visibility.Collapsed;
                commandBarInsert.Visibility = Visibility.Collapsed;
                commandBarView.Visibility = Visibility.Collapsed;
                commandBarHelp.Visibility = Visibility.Collapsed;

                switch (i)
                {
                    case 0:
                        commandBarFile.Visibility = Visibility.Visible;
                        break;
                    case 1:
                        commandBarInsert.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        commandBarView.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        commandBarHelp.Visibility = Visibility.Visible;
                        break;
                }
            }
            else
                AppView.current.ChangePage(AppView.Pages.Settings);
        }
        #endregion

        #region FILE
        private void OnAutosaveToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleAutosaveCommand.Execute(null);
        }
        #endregion

        #region INSERT
        private void OnChapterAddButton_Click(object sender, RoutedEventArgs e)
        {
            if(_chaptersListViewModel.CanAdd)
                _chapterWorkflow.OpenCreateChapterDialog();
        }

        private void OnDialoguesEnableButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.ChapterText.DialoguesOnOff((bool)dialoguesEnableButton.IsChecked);
        }

        private void OnDialoguesAddButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.ChapterText.AddDialogue();
        }

        private void OnDictationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_projectState.Chapters.Count == 0)
            {
                _projectState.AddChapter(ProjectState.GetRandomChapterName());
                _textEditor.SelectedChapterIndex = _projectState.Chapters.Count - 1;
            }

            _textEditor.Focus();

            if (_speechHub.ToggleDictationCommand.CanExecute(null))
            {
                _speechHub.ToggleDictationCommand.Execute(null);
            }
        }
        #endregion

        #region FORMAT
        private void OnFormatterButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Control).Tag?.ToString())
            {
                case "Bold":
                    MainPage.ChapterText.BoldChapterTextBox();
                    break;
                case "Italic":
                    MainPage.ChapterText.ItalicChapterTextBox();
                    break;
                case "Underline":
                    MainPage.ChapterText.UnderlineChapterTextBox();
                    break;
                case "Strikethrough":
                    MainPage.ChapterText.StrikethroughChapterTextBox();
                    break;
                case "Highlighter":
                    MainPage.ChapterText.MarkTextBackground();
                    break;
            }
        }

        private void OnMainHighlighterButton_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (!mainHighlighterFlyout.IsOpen)
                mainHighlighterFlyout.ShowAt(mainHighlighterButton);
            else
                mainHighlighterFlyout.Hide();
        }

        private void OnHighlighterColorButton_Click(object sender, RoutedEventArgs e)
        {
            TextHighlighter.SelectedTool = (TextHighlighter.Tool)Enum.Parse(typeof(TextHighlighter.Tool), (sender as Button).Tag.ToString());
            MainPage.ChapterText.MarkTextBackground();
            mainHighlighterFlyout.Hide();
        }

        public void SetFormattingCommandsEnabled(bool enabled)
        {
            mainBoldButton.IsEnabled = enabled;
            mainItalicButton.IsEnabled = enabled;
            mainUnderlineButton.IsEnabled = enabled;
            mainStrikethroughButton.IsEnabled = enabled;
            mainHighlighterButton.IsEnabled = enabled;
        }

        public void ClearFormattingCommandState()
        {
            mainBoldButton.IsChecked = false;
            mainItalicButton.IsChecked = false;
            mainUnderlineButton.IsChecked = false;
            mainStrikethroughButton.IsChecked = false;
        }

        public bool IsFormattingContextElement(DependencyObject element)
        {
            if (element == null)
                return false;

            return IsChildOf(element, mainBoldButton)
                || IsChildOf(element, mainItalicButton)
                || IsChildOf(element, mainUnderlineButton)
                || IsChildOf(element, mainStrikethroughButton)
                || IsChildOf(element, mainHighlighterButton)
                || IsChildOf(element, typewriterModeButton)
                || IsChildOf(element, mainHighlighterFlyout.Content as DependencyObject);
        }

        private void OnFormattingSurface_GotFocus(object sender, RoutedEventArgs e)
            => MainPage.Current?.SetTextFormattingContextActive(true);

        private void OnFormattingSurface_LostFocus(object sender, RoutedEventArgs e)
        {
            var focused = Windows.UI.Xaml.Input.FocusManager.GetFocusedElement() as DependencyObject;
            if (IsFormattingContextElement(focused)
                || (MainPage.ChapterText?.IsFormattingContextElement(focused) ?? false))
            {
                MainPage.Current?.SetTextFormattingContextActive(true);
                return;
            }

            MainPage.Current?.SetTextFormattingContextActive(false);
        }

        private static bool IsChildOf(DependencyObject child, DependencyObject parent)
        {
            var current = child;
            while (current != null)
            {
                if (current == parent)
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }
        #endregion

        #region VIEW
        private void OnTypewriterModeButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.ChapterText.IsTypewriterModeActive = typewriterModeButton.IsChecked == true;

            if (_textEditor.SelectedChapterIndex >= 0)
                _textEditor.Focus();
        }

        private void OnNotesToggleButton_Click(object sender, RoutedEventArgs e)
            => MainPage.Current.ToggleNotesPane(notesToggleButton.IsChecked == true);

        private void OnSearchReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (searchReplaceButton.IsChecked == true)
                MainPage.ChapterText.OpenSearchAndReplace();
            else
                MainPage.ChapterText.CloseSearchAndReplace();
        }

        private void OnPinboardButton_Click(object sender, RoutedEventArgs e)
            => _navigation.NavigateTo(Services.Interfaces.NavigationTarget.Pinboard);

        private void OnGlobalSearchButton_Click(object sender, RoutedEventArgs e)
            => _ = GlobalSearchDialogue.OpenAsync();

        private void OnWritingPromptsButton_Click(object sender, RoutedEventArgs e)
            => _ = WritingPromptsDialogue.OpenAsync();
        #endregion

        #region HELP
        #region ReadAloud
        private DispatcherTimer timer;
        private CancellationTokenSource _readAloudCts;
        private List<string> _paragraphs;
        private int _currentParagraphIndex;
        private int _currentReadChapterIndex;

        private void OnReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            var speechText = _textEditor.GetText(Services.Interfaces.TextFormat.PlainText);
            if (string.IsNullOrWhiteSpace(speechText))
                return;

            if (readAloudMediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Stopped || readAloudMediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Closed)
                ReadAloud();
        }

        private void OnReadAloudTimer_Tick(object sender, object e)
        {
            if (readAloudMediaElement.NaturalDuration.HasTimeSpan)
            {
                readAloudProgressBar.Maximum = readAloudMediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                readAloudProgressBar.Value = readAloudMediaElement.Position.TotalSeconds;
            }
        }

        public void ReadAloud()
        {
            var speechText = _textEditor.GetText(Services.Interfaces.TextFormat.PlainText);
            if (string.IsNullOrWhiteSpace(speechText))
                return;

            // Split into paragraphs for navigation
            _paragraphs = new List<string>();
            foreach (var p in speechText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = p.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    _paragraphs.Add(trimmed);
            }

            if (_paragraphs.Count == 0) return;

            _currentParagraphIndex = 0;
            _currentReadChapterIndex = _textEditor.SelectedChapterIndex;

            PlayCurrentParagraph();
        }

        private void PlayCurrentParagraph()
        {
            if (_paragraphs == null || _currentParagraphIndex >= _paragraphs.Count)
            {
                // Try to advance to next chapter
                if (TryAdvanceToNextChapter())
                    return;

                StopReadAloud();
                return;
            }

            var text = _paragraphs[_currentParagraphIndex];
            _ = SpeakTextAsync(text);

            timer?.Stop();
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += OnReadAloudTimer_Tick;
            timer.Start();

            readAloudProgressBar.ShowPaused = false;
            readAloudControllHolder.Visibility = Visibility.Visible;
            pauseReadAloud.IsEnabled = true;
            playReadAloud.IsEnabled = false;
            readAloudProgressBar.Value = 0;

            _speechService?.NotifyReadingStarted();
            NotificationManager.DisplayBadgeNotification("playing");
        }

        private bool TryAdvanceToNextChapter()
        {
            var nextIndex = _currentReadChapterIndex + 1;
            if (nextIndex >= _projectState.Chapters.Count)
                return false;

            _currentReadChapterIndex = nextIndex;

            // Select the next chapter in the UI
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (MainPage.ChapterList?.listView != null && nextIndex < MainPage.ChapterList.listView.Items.Count)
                    MainPage.ChapterList.listView.SelectedIndex = nextIndex;
            });

            // Small delay for chapter load, then start reading
            var delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            delayTimer.Tick += (s, args) =>
            {
                delayTimer.Stop();
                var text = _textEditor.GetText(Services.Interfaces.TextFormat.PlainText);
                if (string.IsNullOrWhiteSpace(text))
                {
                    StopReadAloud();
                    return;
                }

                _paragraphs = new List<string>();
                foreach (var p in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = p.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        _paragraphs.Add(trimmed);
                }
                _currentParagraphIndex = 0;
                PlayCurrentParagraph();
            };
            delayTimer.Start();
            return true;
        }

        public async Task SpeakTextAsync(string speechText)
        {
            // Cancel any previous speech
            _readAloudCts?.Cancel();
            _readAloudCts = new CancellationTokenSource();
            var token = _readAloudCts.Token;

            if (string.IsNullOrWhiteSpace(speechText)) return;

            try
            {
                var synth = new SpeechSynthesizer();

                foreach (var voice in SpeechSynthesizer.AllVoices)
                {
                    if (voice.Id == (ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVoice] == null ?
                        SpeechSynthesizer.DefaultVoice.Id : ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVoice].ToString()))
                        synth.Voice = voice;
                }

                if (token.IsCancellationRequested) return;

                var speechStream = await synth.SynthesizeTextToStreamAsync(speechText);

                if (token.IsCancellationRequested)
                {
                    speechStream?.Dispose();
                    return;
                }

                readAloudMediaElement.SetSource(speechStream, speechStream.ContentType);
                var vol = Convert.ToDouble(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVolume] ?? 75);
                if (vol > 0) vol /= 100;
                readAloudMediaElement.Volume = vol;
                readAloudMediaElement.Play();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReadAloud error: {ex.Message}");
            }
        }

        private void StopReadAloud()
        {
            _readAloudCts?.Cancel();
            _readAloudCts = null;

            if (readAloudMediaElement.CurrentState != Windows.UI.Xaml.Media.MediaElementState.Stopped)
                readAloudMediaElement.Stop();

            timer?.Stop();
            timer = null;
            _paragraphs = null;

            readAloudControllHolder.Visibility = Visibility.Collapsed;
            _speechService?.NotifyReadingStopped();
            NotificationManager.ClearBadgeNotification();
        }

        private void OnStopButton_Click(object sender, RoutedEventArgs e)
        {
            StopReadAloud();
        }

        private void OnPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (readAloudMediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Paused)
            {
                readAloudMediaElement.Play();
                readAloudProgressBar.ShowPaused = false;
                pauseReadAloud.IsEnabled = true;
                playReadAloud.IsEnabled = false;

                NotificationManager.DisplayBadgeNotification("playing");
            }
        }

        private void OnPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (readAloudMediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Playing)
            {
                readAloudMediaElement.Pause();
                readAloudProgressBar.ShowPaused = true;
                pauseReadAloud.IsEnabled = false;
                playReadAloud.IsEnabled = true;

                NotificationManager.DisplayBadgeNotification("paused");
            }
        }

        private void OnNextParagraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (_paragraphs == null) return;

            _readAloudCts?.Cancel();
            readAloudMediaElement.Stop();

            _currentParagraphIndex++;
            PlayCurrentParagraph();
        }

        private void OnReadAloudMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Auto-advance to next paragraph
            if (_paragraphs != null)
            {
                _currentParagraphIndex++;
                PlayCurrentParagraph();
            }
            else
            {
                StopReadAloud();
            }
        }
        #endregion

        #endregion
    }
}
