using Storylines.Views.Pages;
using Storylines.Models;
using System;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Storylines.Services;
using Storylines.Services.Interfaces;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ProjectRenamerDialogue : AppContentDialog
    {
        private static IProjectPersistenceService Persistence => App.GetService<IProjectPersistenceService>();

        public ProjectRenamerDialogue()
        {
            this.InitializeComponent();
            CloseOnOutsideTap = true;
        }

        public static async System.Threading.Tasks.Task Open()
        {
            await App.GetService<IDialogService>().ShowAsync(new ProjectRenamerDialogue());
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (string.IsNullOrEmpty(Persistence.CurrentProject?.projectName))
                titleText.Text = ResourceLoader.GetForViewIndependentUse().GetString("chapterDialogueCreate");
            else
                titleText.Text = ResourceLoader.GetForViewIndependentUse().GetString("chapterDialogueRename");

            chapterNameBox.Text = Persistence.CurrentProject?.projectName;
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (Persistence.CurrentProject is not null)
                Persistence.CurrentProject.projectName = chapterNameBox.Text;
            App.GetService<EventAggregator>().Publish(new TitleBarUpdateEvent());

            Hide();
            //_ = MainPage.ChapterText.textBox.Focus(FocusState.Keyboard);
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
    }
}
