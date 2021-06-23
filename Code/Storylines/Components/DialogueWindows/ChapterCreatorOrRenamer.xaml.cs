using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class ChapterCreatorOrRenamer : ContentDialog
    {
        public static ChapterCreatorOrRenamer chapterCreator;

        public enum Task { Create, Rename };
        public static Task currentTask = Task.Create;

        public static Chapter chapterToRename;

        public ChapterCreatorOrRenamer()
        {
            this.InitializeComponent();
            chapterCreator = this;

            MainPage.currentlyOpenedDialogue = chapterCreator;
            chapterCreator.RequestedTheme = MainPage.mainPage.RequestedTheme;
        }

        public static async System.Threading.Tasks.Task Open(Chapter chapter)
        {
            if (chapter != null)
            {
                currentTask = Task.Rename;
                chapterToRename = chapter;
            }
            else
            {
                currentTask = Task.Create;
            }

            await new ChapterCreatorOrRenamer().ShowAsync();
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            switch (currentTask)
            {
                case Task.Create:
                    titleText.Text = "Create chapter";
                    break;
                case Task.Rename:
                    titleText.Text = "Rename chapter";
                    chapterNameBox.Text = chapterToRename.name;
                    break;
            }
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (chapterNameBox.Text != "" && chapterNameBox.Text != null)
            {
                switch (currentTask)
                {
                    case Task.Create:
                        Chapter.Add($"Chapter {MainPage.chapterList.chapters.Count + 1}: {chapterNameBox.Text}");
                        break;
                    case Task.Rename:
                        Chapter.Rename(chapterToRename.token, chapterNameBox.Text);
                        break;
                }
                chapterCreator.Hide();
            }
            else
            {
                chapterNameBox.PlaceholderForeground = new SolidColorBrush(new Color() { A = 255, R = 252, B = 3, G = 40 });
            }
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            chapterCreator.Hide();
        }

        private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                OnSubmitButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            MainPage.currentlyOpenedDialogue = null;
        }
    }
}
