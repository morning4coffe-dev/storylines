using Storylines.Views.Pages;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Storylines.Views.Dialogs;

namespace Storylines.Views.Controls
{
    public sealed partial class MainCommandBar : UserControl
    {
        private readonly ChaptersListViewModel _chaptersListViewModel;
        private readonly IDialogService _dialogs;
        private readonly INavigationService _navigation;
        private readonly ProjectState _projectState;
        private readonly ITextEditorService _textEditor;
        private readonly CommandBarViewModel _viewModel;

        public CommandBarViewModel ViewModel => _viewModel;

        public MainCommandBar()
        {
            this.InitializeComponent();
            _chaptersListViewModel = App.GetService<ChaptersListViewModel>();
            _dialogs = App.GetService<IDialogService>();
            _navigation = App.GetService<INavigationService>();
            _projectState = App.GetService<ProjectState>();
            _textEditor = App.GetService<ITextEditorService>();
            _viewModel = App.GetService<CommandBarViewModel>();

            if(MainPage.FocusMode == null && MainPage.ReadMode == null)
                MainPage.CommandBar = this;
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
                _dialogs.OpenChapterCreator();
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
                _projectState.AddChapter(Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("chapterWithoutName"));
                _textEditor.SelectedChapterIndex = _projectState.Chapters.Count - 1;
            }

            _textEditor.Focus();

            InputInjector inputInjector = InputInjector.TryCreate();

            InjectedInputKeyboardInfo win = new InjectedInputKeyboardInfo
            {
                VirtualKey = (ushort)VirtualKey.LeftWindows,
                KeyOptions = InjectedInputKeyOptions.None
            };


            InjectedInputKeyboardInfo h = new InjectedInputKeyboardInfo
            {
                VirtualKey = (ushort)VirtualKey.H,
                KeyOptions = InjectedInputKeyOptions.None
            };


            inputInjector.InjectKeyboardInput(new[] { win, h });
        }
        #endregion

        #region VIEW
        private void OnNotesToggleButton_Click(object sender, RoutedEventArgs e)
            => MainPage.Current.ToggleNotesPane(notesToggleButton.IsChecked == true);

        private void OnSearchReplaceButton_Click(object sender, RoutedEventArgs e)
            => MainPage.ChapterText.OpenSearchAndReplace();

        private void OnPinboardButton_Click(object sender, RoutedEventArgs e)
            => _navigation.NavigateTo(Services.Interfaces.NavigationTarget.Pinboard);

        private void OnGlobalSearchButton_Click(object sender, RoutedEventArgs e)
            => GlobalSearchDialogue.Open();

        private void OnWritingPromptsButton_Click(object sender, RoutedEventArgs e)
            => WritingPromptsDialogue.Open();
        #endregion

        #region HELP
        #region ReadAloud
        private DispatcherTimer timer;

        private void OnReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (readAloudMediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Stopped || readAloudMediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Closed)
                ReadAloud();
        }

        private void OnReadAloudTimer_Tick(object sender, object e)
        {
            readAloudProgressBar.Maximum = readAloudMediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            readAloudProgressBar.Value = (double)readAloudMediaElement.Position.TotalSeconds;
        }

        public void ReadAloud()
        {
            var speechText = _textEditor.GetText(Services.Interfaces.TextFormat.PlainText);
            if (speechText.Length > 0)
            {
                _ = ToReadAsync(speechText);

                timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };

                timer.Tick += OnReadAloudTimer_Tick;
                timer.Start();
                readAloudProgressBar.ShowPaused = false;

                readAloudControllHolder.Visibility = Visibility.Visible;
                pauseReadAloud.IsEnabled = true;
                playReadAloud.IsEnabled = false;

                readAloudProgressBar.Value = 0;

                NotificationManager.DisplayBadgeNotification("playing");
            }
        }

        public async Task ToReadAsync(string speechText)
        {
            if (speechText != "")
            {
                var synth = new SpeechSynthesizer();

                foreach (var voice in SpeechSynthesizer.AllVoices)
                {
                    if (voice.Id == (ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVoice] == null ?
                        SpeechSynthesizer.DefaultVoice.Id : ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVoice].ToString()))//udelat lepe, dat to settingsvalues variable s {get; set...}
                        synth.Voice = voice;
                }

                var speechStream = await synth.SynthesizeTextToStreamAsync(speechText);

                readAloudMediaElement.SetSource(speechStream, speechStream.ContentType);
                var vol = Convert.ToDouble(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVolume] ?? 75);
                if (vol > 0)
                    vol /= 100;
                readAloudMediaElement.Volume = vol;
                readAloudMediaElement.Play();
            }
        }

        private void OnStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (readAloudMediaElement.CurrentState != Windows.UI.Xaml.Media.MediaElementState.Stopped)
            {
                readAloudMediaElement.Stop();
                timer.Stop();

                readAloudControllHolder.Visibility = Visibility.Collapsed;

                NotificationManager.ClearBadgeNotification();
            }
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

        private void OnReadAloudMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            OnStopButton_Click(sender, new RoutedEventArgs());
        }
        //private void OnNextChapterButton_Click(object sender, RoutedEventArgs e)
        //{
        //    readAloudMediaElement.Pause();
        //}
        #endregion

        #endregion
    }
}
