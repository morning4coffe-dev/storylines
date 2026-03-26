using Storylines.Scripts.Services;
using System.Linq;
using Windows.Storage;
using Windows.UI;
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

            addChapterOnPageDownToggleSwitch.IsOn = System.Convert.ToBoolean(localSettings.Values[SettingsValueStrings.OnPageDownNewChapterEnabled] ?? true);

            // Font settings
            var savedFont = SettingsValues.editorFontFamily;
            fontFamilyComboBox.SelectedItem = fontFamilyComboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == savedFont)
                ?? fontFamilyComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault();
            fontSizeNumBox.Value = SettingsValues.editorFontSize;

            loading = false;
        }

        private void OnThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                MainPage.ChapterList.switchedChapters = Scripts.Functions.TimeTravelSystem.unSavedProgress != true;
                ThemeSettings.ChangeTheme(themeComboBox.SelectedIndex, ThemeSettings.themeListener.CurrentTheme.ToElementTheme());
                //SettingsPage.settings.SwitchPage("Personalize");
            }
        }

        private void OnAccentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                var csa = (SettingsValues.SelectedAccent)accentComboBox.SelectedIndex;
                _ = MainPage.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
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

        private void OnAccentPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                var color = Microsoft.Toolkit.Uwp.Helpers.ColorHelper.ToColor(hex);
                accentComboBox.SelectedIndex = 2; // Custom
                customAccentPicker.Color = color;
                SettingsValues.customAccentColor = color;
                SettingsValues.selectedAccent = SettingsValues.SelectedAccent.Custom;
                ThemeSettings.InitializeAppAccentColor();
            }
        }

        private void OnFontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading && fontFamilyComboBox.SelectedItem is ComboBoxItem item)
            {
                string fontFamily = item.Tag?.ToString() ?? "Calibri";
                localSettings.Values[SettingsValueStrings.EditorFontFamily] = fontFamily;
                ServiceLocator.Events.Publish(new SettingChangedEvent
                {
                    SettingKey = SettingsValueStrings.EditorFontFamily,
                    Value = fontFamily
                });
            }
        }

        private void OnFontSizeNumBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!loading && !double.IsNaN(fontSizeNumBox.Value))
            {
                double size = fontSizeNumBox.Value;
                localSettings.Values[SettingsValueStrings.EditorFontSize] = size;
                ServiceLocator.Events.Publish(new SettingChangedEvent
                {
                    SettingKey = SettingsValueStrings.EditorFontSize,
                    Value = size
                });
            }
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
