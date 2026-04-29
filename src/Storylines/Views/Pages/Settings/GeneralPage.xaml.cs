using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.Globalization;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Views.Pages.Settings
{
    public sealed partial class GeneralPage : Page
    {
        private IProjectPersistenceService Persistence => App.GetService<IProjectPersistenceService>();

        public static GeneralPage current;

        private bool loading;
        readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public GeneralPage()
        {
            loading = true;
            this.InitializeComponent();
            current = this;
        }

        private void GeneralPage_Loaded(object sender, RoutedEventArgs e)
        {
            chapterNameBox.Text = ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ChapterName] != null ? 
                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ChapterName].ToString() : ResourceLoader.GetForCurrentView().GetString("chapterName");
            projectOnStartToggleSwitch.IsOn = ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] != null;
            exitDialogueToggleSwitch.IsOn = SettingsValues.exitDiagEnabled;
            autosaveToggleSwitch.IsOn = SettingsValues.autosaveEnabled;
            foreach (var item in autosaveIntervalComboBox.Items)
            {
                if (Convert.ToDouble((item as ComboBoxItem).Tag, CultureInfo.InvariantCulture) == SettingsValues.autosaveInterval)
                    autosaveIntervalComboBox.SelectedItem = item;
            }
            dailyWordGoalNumBox.Value = SettingsValues.dailyWordGoal;
            experimentalFeaturesToggleSwitch.IsOn = SettingsValues.experimentalFeaturesEnabled;
            loading = false;
        }

        private void OnChapterNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!loading)
                localSettings.Values[SettingsValueStrings.ChapterName] = chapterNameBox.Text;
        }

        private void OnExitDialogueToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loading)
                localSettings.Values[SettingsValueStrings.ExitDialogueOn] = exitDialogueToggleSwitch.IsOn;
        }

        private void OnProjectOnStartupToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loading)
                localSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] = projectOnStartToggleSwitch.IsOn ? Persistence.CurrentProject?.Token : null;
        }

        private void OnAutosaveToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loading)
            {
                if (autosaveToggleSwitch.IsOn && Persistence.CurrentProject?.file == null)
                {
                    autosaveToggleSwitch.IsOn = false;
                    return;
                }

                if (autosaveToggleSwitch.IsOn)
                    Persistence.EnableAutosave();
                else
                    Persistence.DisableAutosave();
            }
        }

        private void OnAutosaveIntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                localSettings.Values[SettingsValueStrings.AutosaveInterval] = Convert.ToDouble((autosaveIntervalComboBox.SelectedItem as ComboBoxItem).Tag, CultureInfo.InvariantCulture);
                if (SettingsValues.autosaveEnabled)
                    Persistence.RefreshAutosave();
            }
        }

        private void OnDailyWordGoalNumBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!loading && !double.IsNaN(dailyWordGoalNumBox.Value))
                localSettings.Values[SettingsValueStrings.DailyWordGoal] = (int)dailyWordGoalNumBox.Value;
        }

        private void GeneralPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth < 1070)
                generalPage.Width = ActualWidth - 70;
            else
                generalPage.Width = 1000;

            if (ActualWidth < 600)
                chapterNameStack.Orientation = Orientation.Vertical;
            else
                chapterNameStack.Orientation = Orientation.Horizontal;
        }

        private void ResetChapterNameButton_Click(object sender, RoutedEventArgs e)
        {
            if (!loading)
            {
                chapterNameBox.Text = ResourceLoader.GetForCurrentView().GetString("chapterName");
                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ChapterName] = ResourceLoader.GetForCurrentView().GetString("chapterName");
            }
        }

        private void OnExperimentalFeaturesToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loading)
            {
                localSettings.Values[SettingsValueStrings.ExperimentalFeaturesEnabled] = experimentalFeaturesToggleSwitch.IsOn;
                App.GetService<EventAggregator>().Publish(new SettingChangedEvent
                {
                    SettingKey = SettingsValueStrings.ExperimentalFeaturesEnabled,
                    Value = experimentalFeaturesToggleSwitch.IsOn
                });
            }
        }
    }
}
