using Storylines.Views.Pages;
using Storylines.Models;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Storylines.Services;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ChapterCreatorOrRenamer : ContentDialog
    {
        public static ChapterCreatorOrRenamer chapterCreator;

        public enum Task { Create, Rename };
        public static Task currentTask = Task.Create;

        public static Chapter chapterToRename;

        private static bool doubleTapped;

        public ChapterCreatorOrRenamer()
        {
            this.InitializeComponent();
            chapterCreator = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = chapterCreator;
            chapterCreator.RequestedTheme = AppView.current.ActualTheme;
        }

        public static void Open(Chapter chapter, bool doubleTap)
        {
            if (chapter != null)
            {
                currentTask = Task.Rename;
                chapterToRename = chapter;
            }
            else
                currentTask = Task.Create;

            doubleTapped = doubleTap;
            _ = new ChapterCreatorOrRenamer().ShowAsync();
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
                    chapterNameBox.Text = chapterToRename.Name;
                    break;
            }
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var chapterWorkflow = App.GetService<Storylines.Services.Interfaces.IChapterWorkflowService>();

            switch (currentTask)
            {
                case Task.Create:
                    chapterWorkflow.CreateChapterFromInput(chapterNameBox.Text);
                    break;
                case Task.Rename:
                    chapterWorkflow.RenameChapter(chapterToRename.Token, chapterNameBox.Text);
                    break;
            }
            chapterCreator.Hide();
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
            Window.Current.CoreWindow.PointerPressed -= OnWindowPointerPressed;
            AppView.currentlyOpenedDialogue = null;

            if (ReferenceEquals(chapterCreator, this))
                chapterCreator = null;
        }

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            Window.Current.CoreWindow.PointerPressed += OnWindowPointerPressed;

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }

        private void OnWindowPointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (isHide && !doubleTapped)
                Hide();
        }
    }
}
