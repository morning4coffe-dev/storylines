using Storylines.Views.Pages;
using Storylines.Models;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Storylines.Services;
using Storylines.Services.Interfaces;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ChapterCreatorOrRenamer : StorylinesContentDialog
    {
        public enum DialogTask { Create, Rename }

        private readonly Chapter _chapterToRename;
        private readonly bool _doubleTapped;
        private readonly DialogTask _currentTask;

        public ChapterCreatorOrRenamer(Chapter chapterToRename, bool doubleTapped)
        {
            this.InitializeComponent();
            CloseOnOutsideTap = true;
            _chapterToRename = chapterToRename;
            _doubleTapped = doubleTapped;
            _currentTask = chapterToRename == null ? DialogTask.Create : DialogTask.Rename;
        }

        public static void Open(Chapter chapter, bool doubleTap)
        {
            _ = OpenAsync(chapter, doubleTap);
        }

        public static Task<ContentDialogResult> OpenAsync(Chapter chapter, bool doubleTap)
        {
            return App.GetService<IDialogService>().ShowAsync(new ChapterCreatorOrRenamer(chapter, doubleTap));
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            switch (_currentTask)
            {
                case DialogTask.Create:
                    titleText.Text = ResourceLoader.GetForViewIndependentUse().GetString("chapterDialogueCreate");
                    break;
                case DialogTask.Rename:
                    titleText.Text = ResourceLoader.GetForViewIndependentUse().GetString("chapterDialogueRename");
                    chapterNameBox.Text = _chapterToRename.Name;
                    break;
            }
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var chapterWorkflow = App.GetService<Storylines.Services.Interfaces.IChapterWorkflowService>();

            switch (_currentTask)
            {
                case DialogTask.Create:
                    chapterWorkflow.CreateChapterFromInput(chapterNameBox.Text);
                    break;
                case DialogTask.Rename:
                    chapterWorkflow.RenameChapter(_chapterToRename.Token, chapterNameBox.Text);
                    break;
            }
            Hide();
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && submitButton.IsEnabled)
                OnSubmitButton_Click(sender, new RoutedEventArgs());
        }

        protected override bool CanCloseOnOutsideTap()
        {
            return !_doubleTapped;
        }
    }
}
