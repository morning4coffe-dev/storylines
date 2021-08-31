using Storylines.Pages;
using Storylines.Scripts.Variables;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

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

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = chapterCreator;
            chapterCreator.RequestedTheme = AppView.current.RequestedTheme;
        }

        public static async System.Threading.Tasks.Task Open(Chapter chapter)
        {
            if (chapter != null)
            {
                currentTask = Task.Rename;
                chapterToRename = chapter;
            }
            else
                currentTask = Task.Create;

            await new ChapterCreatorOrRenamer().ShowAsync();
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            switch (currentTask)
            {
                case Task.Create:
                    titleText.Text = ResourceLoader.GetForCurrentView().GetString("chapterDialogueCreate");
                    break;
                case Task.Rename:
                    titleText.Text = ResourceLoader.GetForCurrentView().GetString("chapterDialogueRename");
                    chapterNameBox.Text = chapterToRename.name;
                    break;
            }
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            switch (currentTask)
            {
                case Task.Create:
                    var itemID = MainPage.chapterList.listView.Items.Count;
                    Chapter.AddFromCreator(Chapter.chapters.Count + 1, chapterNameBox.Text);
                    MainPage.chapterList.CheckForEmptyList();
                    MainPage.chapterList.listView.SelectedIndex = itemID;
                    break;
                case Task.Rename:
                    Chapter.Rename(chapterToRename.token, chapterNameBox.Text);
                    break;
            }
            chapterCreator.Hide();
            //_ = MainPage.chapterText.textBox.Focus(FocusState.Keyboard);
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            chapterCreator.Hide();
        }

        private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && submitButton.IsEnabled)
                OnSubmitButton_Click(sender, new RoutedEventArgs());
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            AppView.currentlyOpenedDialogue = null;
        }

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            Window.Current.CoreWindow.PointerPressed += (s, e) =>
            {
                if (isHide)
                    Hide();
            };

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }
    }
}
