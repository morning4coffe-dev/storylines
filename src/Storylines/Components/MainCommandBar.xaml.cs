using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components
{
    public sealed partial class MainCommandBar : UserControl
    {
        private readonly string feedbackLink = "https://github.com/morning4coffe-dev/Storylines/issues/new";
        //private readonly string shortcutsLink = "https://github.com/morning4coffe-dev/storylines/blob/main/shortcuts.md";

        public MainCommandBar()
        {
            this.InitializeComponent();
            if(MainPage.focusMode == null && MainPage.readMode == null)
                MainPage.commandBar = this;

            autosaveToggleButton.IsChecked = SettingsValues.autosaveEnabled;
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
        private void OnUndoButton_Click(object sender, RoutedEventArgs e)
        {
            TimeTravelChapter.Undo();
        }

        private void OnRedoButton_Click(object sender, RoutedEventArgs e)
        {
            TimeTravelChapter.Redo();
        }

        private void OnSaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSystem.Save();
        }

        private void OnSaveCopyButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSystem.SaveCopy();
        }

        private void OnLoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProjectDialogue.loadFile.isEscape = false;
            LoadProjectDialogue.Open();
        }

        private void OnExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportDialogue.Open(default);
        }

        private void OnAutosaveToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)autosaveToggleButton.IsChecked)
                Autosave.Enable();
            else
                Autosave.Disable();
        }
        #endregion

        #region INSERT
        private void OnChapterAddButton_Click(object sender, RoutedEventArgs e)
        {
            if(MainPage.chapterList.canAdd)
                ChapterCreatorOrRenamer.Open(null, false);
        }

        private void OnDialoguesEnableButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.chapterText.DialoguesOnOff((bool)dialoguesEnableButton.IsChecked);

            //dialoguesAddButton.IsEnabled = (bool)dialoguesEnableButton.IsChecked;
        }

        private void OnDialoguesAddButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.chapterText.AddDialogue();
        }

        private void OnDialoguesAddSimpleButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.chapterText.AddSimpleDialogue();
        }

        private void OnDictationButton_Click(object sender, RoutedEventArgs e)
        {
            //NotificationManager.DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational, "This feature has been temporarily disabled.", "Sorry, this feature has been disabled, but it will be brought back with the next update. Currently, you can add a chapter and press Win+H to type with your voice.");

            if (Chapter.chapters.Count == 0)
            {
                Chapter.Add(Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("chapterWithoutName"));
                MainPage.chapterList.listView.SelectedIndex = Chapter.chapters.Count - 1;
            }

            _ = MainPage.chapterText.textBox.Focus(FocusState.Pointer);

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
        private void OnReadModeButton_Click(object sender, RoutedEventArgs e)
        {
            ModesDialogue.Open(ModesDialogue.ModeType.Read);
        }

        private void OnFocusModeButton_Click(object sender, RoutedEventArgs e)
        {
            ModesDialogue.Open(ModesDialogue.ModeType.Focus);
        }

        private void OnCharactersButton_Click(object sender, RoutedEventArgs e)
        {
            AppView.current.ChangePage(AppView.Pages.Characters);
        }
        #endregion

        #region HELP
        private void OnFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            //FeedbackDialogue.Open();
            //if (Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported())
            //{
            //    _ = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault().LaunchAsync();
            //}
            _ = Launcher.LaunchUriAsync(new Uri(feedbackLink));
        }

        private void OnShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            //_ = Launcher.LaunchUriAsync(new Uri(shortcutsLink));
            ShortcutsDialogue.Open();
        }

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
            MainPage.chapterText.textBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string speechText);
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
