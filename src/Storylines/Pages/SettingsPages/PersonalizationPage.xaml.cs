using Storylines.Scripts.Services;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Pages.SettingsPages
{
    public sealed partial class PersonalizationPage : Page
    {
        public static PersonalizationPage current;

        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private bool loading;

        public PersonalizationPage()
        { 
            loading = true;
            InitializeComponent();
            current = this;
        }

        private void OnPersonalizationPage_Loaded(object sender, RoutedEventArgs e)
        {
            themeComboBox.SelectedIndex = (int)SettingsValues.selectedTheme;

            accentComboBox.SelectedIndex = (int)SettingsValues.selectedAccent;

            customAccentPicker.Color = SettingsValues.customAccentColor;
            customAccentPicker.IsEnabled = SettingsValues.selectedAccent == SettingsValues.SelectedAccent.Custom;

            //textBoxBackgroundToggleSwitch.IsOn = SettingsValues.textBoxSolidBackground;
            addChapterOnPageDownToggleSwitch.IsOn = System.Convert.ToBoolean(localSettings.Values[SettingsValueStrings.OnPageDownNewChapterEnabled] ?? true);
            loading = false;
        }

        private void OnThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                MainPage.chapterList.switchedChapters = Scripts.Functions.TimeTravelSystem.unSavedProgress != true;
                ThemeSettings.ChangeTheme(themeComboBox.SelectedIndex, ThemeSettings.themeListener.CurrentTheme.ToElementTheme());
                //SettingsPage.settings.SwitchPage("Personalize");
            }
        }

        private void OnAccentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                var csa = (SettingsValues.SelectedAccent)accentComboBox.SelectedIndex;
                _ = MainPage.current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    customAccentPicker.IsEnabled = csa == SettingsValues.SelectedAccent.Custom;
                });

                SettingsValues.selectedAccent = csa;
                ThemeSettings.InitializeAppAccentColor();
            }
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            SettingsValues.customAccentColor = sender.Color;
            ThemeSettings.InitializeAppAccentColor();
        }

        private void OnAddChapterOnPageDownToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loading)
                localSettings.Values[SettingsValueStrings.OnPageDownNewChapterEnabled] = addChapterOnPageDownToggleSwitch.IsOn;
        }

        private void OnPersonalizationPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualWidth < 1070)
                personalizationPage.Width = this.ActualWidth - 70;
            else
                personalizationPage.Width = 1000;
        }
    }
}
