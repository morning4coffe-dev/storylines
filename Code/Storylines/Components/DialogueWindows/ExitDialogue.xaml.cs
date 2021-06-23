using Storylines.Pages;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class ExitDialogue : ContentDialog
    {
        public static ExitDialogue exitDialogue;

        public static bool appClosing = false;

        public ExitDialogue()
        {
            this.InitializeComponent();
            exitDialogue = this;

            exitDialogue.RequestedTheme = MainPage.mainPage.RequestedTheme;

            if (MainPage.currentlyOpenedDialogue != null)
            {
                if (MainPage.currentlyOpenedDialogue == LoadFileDialogue.loadFile)
                {
                    LoadFileDialogue.loadFile.isEscape = false;
                }

                MainPage.currentlyOpenedDialogue.Hide();
            }

            MainPage.currentlyOpenedDialogue = exitDialogue;
            //cancelButton.Visibility = appClosing ? Visibility.Visible : Visibility.Collapsed;
        }

        public static async System.Threading.Tasks.Task Open(bool closing)
        {
            appClosing = closing;

            if (SettingsPage.isExitDialogueOn)
                await new ExitDialogue().ShowAsync();
            if (!SettingsPage.isExitDialogueOn)
                App.Current.Exit();
        }

        private void OnSave_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            MainPage.saveSystem.SaveAndExitOrClearAll(appClosing);
        }

        private void OnDontSave_Click(object sender, RoutedEventArgs e)
        {
            if (appClosing)
            {
                App.Current.Exit();
            }
            else
            {
                SaveSystem.saveFile = null;
                MainPage.mainPage.ClearEverything();
                MainPage.mainPage.unSavedProgress = false;

                LoadFileDialogue.Open();
                this.Hide();
            }
        }

        private void OnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            MainPage.currentlyOpenedDialogue = null;
        }
    }
}
