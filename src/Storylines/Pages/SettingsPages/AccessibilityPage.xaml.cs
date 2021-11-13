using Storylines.Scripts.Services;
using Windows.Globalization;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Pages.SettingsPages
{
    public sealed partial class AccessibilityPage : Page
    {
        public static AccessibilityPage current;
        readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private bool loading;

        public AccessibilityPage()
        { 
            loading = true;
            InitializeComponent();
            current = this;
        }

        private void OnAccessibilityPage_Loaded(object sender, RoutedEventArgs e)
        {
            textBoxBackgroundToggleSwitch.IsOn = System.Convert.ToBoolean(localSettings.Values[SettingsValueStrings.TextBoxSolidBackground] ?? false);

            readAloudVolumeSlider.Value = System.Convert.ToDouble(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVolume] ?? 75);

            string currentLang;
            if (SettingsValues.IsUserLanguageSupported())
                currentLang = string.IsNullOrEmpty(ApplicationLanguages.PrimaryLanguageOverride) ? ApplicationLanguages.Languages[0] : ApplicationLanguages.PrimaryLanguageOverride;
            else
                currentLang = "en";

            for (int i = 0; i < langComboBox.Items.Count; i++)
                if (currentLang == (langComboBox.Items[i] as ComboBoxItem).Tag.ToString())
                    langComboBox.SelectedItem = langComboBox.Items[i];

            var voices = SpeechSynthesizer.AllVoices;
            string lastVoice = (ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVoice] ?? SpeechSynthesizer.DefaultVoice.Id).ToString();
            for (int i = 0; i < voices.Count; i++)
            {
                voiceComboBox.Items.Add(new ComboBoxItem() { Content = voices[i].DisplayName, Tag = voices[i].Id });
                if (voices[i].Id == lastVoice)
                    voiceComboBox.SelectedIndex = i;
            }

            loading = false;
        }

        private void OnLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                var lang = (langComboBox.SelectedItem as ComboBoxItem).Tag.ToString();
                ApplicationLanguages.PrimaryLanguageOverride = lang;
                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.UserLanguage] = lang;
            }
        }

        private void OnReadAloudVolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!loading)
            {
                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVolume] = readAloudVolumeSlider.Value;

                ButtonIcon(readAloudVolumeSlider.Value);
            }
        }

        private void VolumeButton_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonIcon(readAloudVolumeSlider.Value);
        }

        private void ButtonIcon(double v)
        {
            if (v == 0)
                readAloudVolumeButtonIcon.Glyph = "";
            else
            if (v > 0 && v < 50)
                readAloudVolumeButtonIcon.Glyph = "";
            else
            if (v >= 50 && v < 100)
                readAloudVolumeButtonIcon.Glyph = "";
            else
                readAloudVolumeButtonIcon.Glyph = "";
        }

        private void OnReadAloudVolumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (readAloudVolumeSlider.Value > 0)
                readAloudVolumeSlider.Value = 0;
            else
                readAloudVolumeSlider.Value = 100;
        }

        private void OnVoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReadAloudVoice] = (voiceComboBox.SelectedItem as ComboBoxItem).Tag.ToString();
        }

        private void OnTextBoxBackgroundToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loading)
            {
                MainPage.chapterList.switchedChapters = Scripts.Functions.TimeTravelSystem.unSavedProgress != true;
                localSettings.Values[SettingsValueStrings.TextBoxSolidBackground] = textBoxBackgroundToggleSwitch.IsOn;
            }
        }

        private void OnAccessibilityPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth < 1070)
                accessibilityPage.Width = ActualWidth - 70;
            else
                accessibilityPage.Width = 1000;

            if (ActualWidth < 600)
            {
                readAloudVolumeStack.Orientation = Orientation.Vertical;
                readAloudVolumeText.Visibility = Visibility.Collapsed;
                readAloudVolumeSlider.Width = 120;
            }
            else
            {
                readAloudVolumeStack.Orientation = Orientation.Horizontal;
                readAloudVolumeText.Visibility = Visibility.Visible;
                readAloudVolumeSlider.Width = 150;
            }
        }
    }
}
