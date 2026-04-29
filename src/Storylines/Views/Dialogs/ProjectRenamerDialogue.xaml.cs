using Storylines.Views.Pages;
using Storylines.Models;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Storylines.Services;
using Storylines.Services.Interfaces;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ProjectRenamerDialogue : ContentDialog
    {
        private static IProjectPersistenceService Persistence => App.GetService<IProjectPersistenceService>();

        public static ProjectRenamerDialogue projectRenamer;

        public ProjectRenamerDialogue()
        {
            this.InitializeComponent();
            projectRenamer = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = projectRenamer;
            projectRenamer.RequestedTheme = AppView.current.ActualTheme;
        }

        public static async System.Threading.Tasks.Task Open()
        {
            await new ProjectRenamerDialogue().ShowAsync();
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (string.IsNullOrEmpty(Persistence.CurrentProject?.projectName))
                titleText.Text = ResourceLoader.GetForCurrentView().GetString("chapterDialogueCreate");
            else
                titleText.Text = ResourceLoader.GetForCurrentView().GetString("chapterDialogueRename");

            chapterNameBox.Text = Persistence.CurrentProject?.projectName;
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (Persistence.CurrentProject != null)
                Persistence.CurrentProject.projectName = chapterNameBox.Text;
            App.GetService<EventAggregator>().Publish(new TitleBarUpdateEvent());

            projectRenamer.Hide();
            //_ = MainPage.ChapterText.textBox.Focus(FocusState.Keyboard);
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            projectRenamer.Hide();
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
