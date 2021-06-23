using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Pages.SettingsPages
{
    public sealed partial class PersonalizationPage : Page
    {
        public static PersonalizationPage current;

        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public PersonalizationPage()
        {
            this.InitializeComponent();
            current = this;
        }

        private void OnPersonalizationPage_Loaded(object sender, RoutedEventArgs e)
        {
            switch (SettingsPage.selectedTheme)
            {
                case SettingsPage.CurrentSelectedTheme.Light:
                    themeSelector0.IsChecked = true;
                    break;
                case SettingsPage.CurrentSelectedTheme.Dark:
                    themeSelector1.IsChecked = true;
                    break;
                case SettingsPage.CurrentSelectedTheme.System:
                    themeSelector2.IsChecked = true;
                    break;
            }

            systemColorToggleSwitch.IsOn = SettingsPage.appColorEnabled;
            textBoxBackgroundToggleSwitch.IsOn = SettingsPage.textBoxSolidBackground;
            exitDialogueToggleSwitch.IsOn = SettingsPage.isExitDialogueOn;
            addChapterOnPageDownToggleSwitch.IsOn = SettingsPage.isOnPageDownNewChapterEnabled;
        }

        private void OnThemeChangeRadioButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.chapterList.switchedChapters = MainPage.mainPage.unSavedProgress != true;
            SettingsPage.ChangeTheme(Convert.ToInt32((sender as RadioButton).Tag));
        }

        private void OnSystemColorToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SettingsPage.UpdateSystemColor(systemColorToggleSwitch.IsOn);
        }

        private void OnTextBoxBackgroundToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            MainPage.chapterList.switchedChapters = MainPage.mainPage.unSavedProgress != true;
            MainPage.chapterText.TextBoxWhiteBackground(textBoxBackgroundToggleSwitch.IsOn);
            SettingsPage.textBoxSolidBackground = textBoxBackgroundToggleSwitch.IsOn;
            //MainPage.chapterText.textBoxBackgroundRectangle.Visibility = textBoxBackgroundToggleSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnExitDialogueToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SettingsPage.isExitDialogueOn = exitDialogueToggleSwitch.IsOn;
        }

        private void OnAddChapterOnPageDownToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SettingsPage.isOnPageDownNewChapterEnabled = addChapterOnPageDownToggleSwitch.IsOn;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            localSettings.Values["AppColor"] = SettingsPage.appColorEnabled;
            localSettings.Values["SolidBackground"] = SettingsPage.textBoxSolidBackground;
            localSettings.Values["ExitDialogue"] = SettingsPage.isExitDialogueOn;
            localSettings.Values["OnPageDownNewChapterEnabled"] = SettingsPage.isOnPageDownNewChapterEnabled;
        }
    }
}
