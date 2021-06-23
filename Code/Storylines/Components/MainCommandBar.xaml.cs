using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components
{
    public sealed partial class MainCommandBar : UserControl
    {
        public MainCommandBar()
        {
            this.InitializeComponent();
            MainPage.commandBar = this;
        }

        private void OnSaveButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.saveSystem.Save();
        }

        private void OnSaveCopyButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.saveSystem.SaveAs();
        }

        private void OnLoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFileDialogue.loadFile.isEscape = false;
            LoadFileDialogue.Open();
        }

        private void OnPrintButton_Click(object sender, RoutedEventArgs e)
        {
            //_ = ExportOrPrintDialogue.Open(true);
            MainPage.saveSystem.Print();
        }

        private void OnExportButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ExportOrPrintDialogue.Open(false);
        }

        private void OnCharactersButton_Click(object sender, RoutedEventArgs e)
        {
            CharactersDialogue.Open();
        }

        private void OnSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.mainPage.OpenSettings(true);
        }

        DispatcherTimer autosaveTimer = new DispatcherTimer();
        private void OnAutosaveToggleButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPage.isAutosaveEnabled = (bool)autosaveToggleButton.IsChecked;
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["AutosaveEnabled"] = (bool)autosaveToggleButton.IsChecked;

            MainPage.mainPage.Autosave();

            if ((bool)autosaveToggleButton.IsChecked)
            {
                autosaveTimer.Tick += autosaveTimer_Tick;
                autosaveTimer.Interval = new TimeSpan(0, 2, 0);
                autosaveTimer.Start();
            }
            else 
            {
                autosaveTimer.Stop();
            }
        }

        private void autosaveTimer_Tick(object sender, object e)
        {
            if (SettingsPage.isAutosaveEnabled)
            {
                MainPage.mainPage.Autosave();
            }
        }
    }
}
